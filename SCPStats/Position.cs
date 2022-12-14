namespace SCPStats
{
    using UnityEngine;
    
    public class Position
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        private Vector3 _vector3 = default;

        public Vector3 GetPositions()
        {
            if (_vector3 == default)
            {
                _vector3 = new Vector3(x, y, z);
            }

            return _vector3;
        }
        
        public Position() {}
        
        public Position(Vector3 vector3)
        {
            x = vector3.x;
            y = vector3.y;
            z = vector3.z;
        }
    }
}