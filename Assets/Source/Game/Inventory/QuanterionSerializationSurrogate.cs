using System.Runtime.Serialization;
using UnityEngine;

namespace Rogue {
    sealed class QuanterionSerializationSurrogate : ISerializationSurrogate {
        public void GetObjectData(System.Object obj,
            SerializationInfo info, StreamingContext context) {
            Quaternion q = (Quaternion) obj;
            info.AddValue("x", q.x);
            info.AddValue("y", q.y);
            info.AddValue("z", q.z);
            info.AddValue("w", q.w);
        }

        public System.Object SetObjectData(System.Object obj,
            SerializationInfo info, StreamingContext context,
            ISurrogateSelector selector) {
		
            Quaternion quaternion = (Quaternion) obj;
            quaternion.x = (float)info.GetValue("x", typeof(float));
            quaternion.y = (float)info.GetValue("y", typeof(float));
            quaternion.z = (float)info.GetValue("z", typeof(float));
            quaternion.w = (float)info.GetValue("w", typeof(float));
            obj = quaternion;
            return obj;
        }
    }
}