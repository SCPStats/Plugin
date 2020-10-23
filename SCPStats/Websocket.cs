using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;

namespace SCPStats
{
    public class Websocket
    {
        private string address;
        public ClientWebSocket ws;
        private Task Listener = null;

        public Action<string> OnMessage = null;
        public Action OnClose = null;

        public Websocket(string address)
        {
            this.address = address;
            ws = new ClientWebSocket();
        }

        public async Task Connect()
        {
            if (ws.State != WebSocketState.None) return;
            await ws.ConnectAsync(new Uri(address), CancellationToken.None);
            Listener = Listen();
        }

        public async Task Send(string data) => await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
            WebSocketMessageType.Text, true, CancellationToken.None);

        private async Task Listen()
        {
            var buffer = new byte[2048];
            while (ws != null && ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    ws.Abort();
                    ws.Dispose();
                    ws = null;
                    OnClose?.Invoke();
                    return;
                }

                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                OnMessage?.Invoke(msg);
            }
            
            ws?.Abort();
            ws?.Dispose();
            ws = null;
            OnClose?.Invoke();
        }

        public async Task Close(bool sendClose = true)
        {
            if (ws.State != WebSocketState.Open)
            {
                Log.Info(ws.State);
                return;
            }
            ws.Abort();
            ws.Dispose();
            ws = null;
            if(sendClose) OnClose?.Invoke();
        }
    }
}