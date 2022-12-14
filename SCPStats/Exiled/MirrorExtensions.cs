// -----------------------------------------------------------------------
// <copyright file="MirrorExtensions.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

using PluginAPI.Core;

namespace SCPStats.Exiled
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    
    using Mirror;
    
    /// <summary>
    /// A set of extensions for <see cref="Mirror"/> Networking.
    /// </summary>
    public static class MirrorExtensions
    {
        private static readonly Dictionary<Type, MethodInfo> WriterExtensionsValue = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<string, ulong> SyncVarDirtyBitsValue = new Dictionary<string, ulong>();
        private static readonly ReadOnlyDictionary<Type, MethodInfo> ReadOnlyWriterExtensionsValue = new ReadOnlyDictionary<Type, MethodInfo>(WriterExtensionsValue);
        private static readonly ReadOnlyDictionary<string, ulong> ReadOnlySyncVarDirtyBitsValue = new ReadOnlyDictionary<string, ulong>(SyncVarDirtyBitsValue);

        /// <summary>
        /// Gets <see cref="MethodInfo"/> corresponding to <see cref="Type"/>.
        /// </summary>
        public static ReadOnlyDictionary<Type, MethodInfo> WriterExtensions
        {
            get
            {
                if (WriterExtensionsValue.Count == 0)
                {
                    foreach (MethodInfo method in typeof(NetworkWriterExtensions).GetMethods().Where(x => !x.IsGenericMethod && x.GetParameters()?.Length == 2))
                        WriterExtensionsValue.Add(method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType, method);

                    foreach (MethodInfo method in typeof(GeneratedNetworkCode).GetMethods().Where(x => !x.IsGenericMethod && x.GetParameters()?.Length == 2 && x.ReturnType == typeof(void)))
                        WriterExtensionsValue.Add(method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType, method);

                    foreach (Type serializer in typeof(ServerConsole).Assembly.GetTypes().Where(x => x.Name.EndsWith("Serializer")))
                    {
                        foreach (MethodInfo method in serializer.GetMethods().Where(x => x.ReturnType == typeof(void) && x.Name.StartsWith("Write")))
                            WriterExtensionsValue.Add(method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType, method);
                    }
                }

                return ReadOnlyWriterExtensionsValue;
            }
        }

        /// <summary>
        /// Gets a all DirtyBit <see cref="ulong"/> from <see cref="StringExtensions"/>(format:classname.methodname).
        /// </summary>
        public static ReadOnlyDictionary<string, ulong> SyncVarDirtyBits
        {
            get
            {
                if (SyncVarDirtyBitsValue.Count == 0)
                {
                    foreach (PropertyInfo property in typeof(ServerConsole).Assembly.GetTypes()
                        .SelectMany(x => x.GetProperties())
                        .Where(m => m.Name.StartsWith("Network")))
                    {
                        MethodInfo setMethod = property.GetSetMethod();
                        if (setMethod is null)
                            continue;
                        MethodBody methodBody = setMethod.GetMethodBody();
                        if (methodBody is null)
                            continue;
                        byte[] bytecodes = methodBody.GetILAsByteArray();
                        if (!SyncVarDirtyBitsValue.ContainsKey($"{property.Name}"))
                            SyncVarDirtyBitsValue.Add($"{property.Name}", bytecodes[bytecodes.LastIndexOf((byte)OpCodes.Ldc_I8.Value) + 1]);
                    }
                }

                return ReadOnlySyncVarDirtyBitsValue;
            }
        }

        /// <summary>
        /// Send fake values to client's <see cref="SyncVarAttribute"/>.
        /// </summary>
        /// <param name="target">Target to send.</param>
        /// <param name="behaviorOwner"><see cref="NetworkIdentity"/> of object that owns <see cref="NetworkBehaviour"/>.</param>
        /// <param name="targetType"><see cref="NetworkBehaviour"/>'s type.</param>
        /// <param name="propertyName">Property name starting with Network.</param>
        /// <param name="value">Value of send to target.</param>
        public static void SendFakeSyncVar(this Player target, NetworkIdentity behaviorOwner, Type targetType, string propertyName, object value)
        {
            void CustomSyncVarGenerator(NetworkWriter targetWriter)
            {
                targetWriter.WriteUInt64(SyncVarDirtyBits[propertyName]);
                WriterExtensions[value.GetType()]?.Invoke(null, new[] { targetWriter, value });
            }

            PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
            PooledNetworkWriter writer2 = NetworkWriterPool.GetWriter();
            MakeCustomSyncWriter(behaviorOwner, targetType, null, CustomSyncVarGenerator, writer, writer2);
            target.ReferenceHub.networkIdentity.connectionToClient.Send(new UpdateVarsMessage() { netId = behaviorOwner.netId, payload = writer.ToArraySegment() });
            NetworkWriterPool.Recycle(writer);
            NetworkWriterPool.Recycle(writer2);
        }

        // Make custom writer(private)
        private static void MakeCustomSyncWriter(NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customSyncObject, Action<NetworkWriter> customSyncVar, NetworkWriter owner, NetworkWriter observer)
        {
            byte behaviorDirty = 0;
            NetworkBehaviour behaviour = null;

            // Get NetworkBehaviors index (behaviorDirty use index)
            for (int i = 0; i < behaviorOwner.NetworkBehaviours.Length; i++)
            {
                if (behaviorOwner.NetworkBehaviours[i].GetType() == targetType)
                {
                    behaviour = behaviorOwner.NetworkBehaviours[i];
                    behaviorDirty = (byte)i;
                    break;
                }
            }

            // Write target NetworkBehavior's dirty
            owner.WriteByte(behaviorDirty);

            // Write init position
            int position = owner.Position;
            owner.WriteInt32(0);
            int position2 = owner.Position;

            // Write custom sync data
            if (!(customSyncObject is null))
                customSyncObject.Invoke(owner);
            else
                behaviour.SerializeObjectsDelta(owner);

            // Write custom syncvar
            customSyncVar?.Invoke(owner);

            // Write syncdata position data
            int position3 = owner.Position;
            owner.Position = position;
            owner.WriteInt32(position3 - position2);
            owner.Position = position3;

            // Copy owner to observer
            if (behaviour.syncMode != SyncMode.Observers)
            {
                ArraySegment<byte> arraySegment = owner.ToArraySegment();
                observer.WriteBytes(arraySegment.Array, position, owner.Position - position);
            }
        }
    }
}