using UnityEngine;

namespace SoftRender
{
    public class VirtualCamera
    {
        public Vector3 Position { set; get; }
        public float Fov { set; get; }
        public float Aspect { set; get; }
        public float Far { set; get; }
        public float Near { set; get; }

        public VirtualCamera(Vector3 pos, float fov, float aspect, float far, float near)
        {
            Position = pos;
            Fov = fov;
            Aspect = aspect;
            Far = far;
            Near = near;
        }

        public Matrix4x4 GetViewMatrix4X4(Vector3 lookAt, Vector3 worldUp)
        {
            var forward = (Position - lookAt).normalized;
            //叉乘顺序
            var right = Vector3.Cross(forward, worldUp).normalized;
            var up = Vector3.Cross(right, forward).normalized;

            var r = new Matrix4x4();
            r.m00 = right.x;
            r.m01 = right.y;
            r.m02 = right.z;
            r.m03 = -Vector3.Dot(right, Position);
            r.m10 = up.x;
            r.m11 = up.y;
            r.m12 = up.z;
            r.m13 = -Vector3.Dot(up, Position);
            r.m20 = forward.x;
            r.m21 = forward.y;
            r.m22 = forward.z;
            r.m23 = -Vector3.Dot(forward, Position);
            r.m33 = 1;
            return r;
        }

        public Matrix4x4 GetProjectionMatrix4X4()
        {
            var rad = Mathf.Deg2Rad * Fov;
            var cot = 1 / Mathf.Tan(rad / 2);

            var r = new Matrix4x4();
            r.m00 = cot / Aspect;
            r.m11 = cot;
            r.m22 = -(Far + Near) / (Far - Near);
            r.m23 = -2 * Near * Far / (Far - Near);
            r.m32 = -1;
            return r;
        }
    }
}
