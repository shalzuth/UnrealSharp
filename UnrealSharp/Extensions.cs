using System;
using System.Numerics;

namespace UnrealSharp
{
    public static class Extensions
    {
        public struct Vector3Double
        {
            public double X;
            public double Y;
            public double Z;
            public Vector3 ToFloats()
            {
                return new Vector3((float)X, (float)Y, (float)Z);
            }
        }
        public static Single Mult(this Vector3 v, Vector3 s) => v.X * s.X + v.Y * s.Y + v.Z * s.Z;
        public static void GetAxes(this Vector3 v, out Vector3 x, out Vector3 y, out Vector3 z)
        {
            var m = v.ToMatrix();

            x = new Vector3(m[0, 0], m[0, 1], m[0, 2]);
            y = new Vector3(m[1, 0], m[1, 1], m[1, 2]);
            z = new Vector3(m[2, 0], m[2, 1], m[2, 2]);
        }
        public static Vector3 FromRotator(this Vector3 v)
        {
            float radPitch = (float)(v.X * Math.PI / 180f);
            float radYaw = (float)(v.Y * Math.PI / 180f);
            float SP = (float)Math.Sin(radPitch);
            float CP = (float)Math.Cos(radPitch);
            float SY = (float)Math.Sin(radYaw);
            float CY = (float)Math.Cos(radYaw);
            return new Vector3(CP * CY, CP * SY, SP);
        }
        public static Single[,] ToMatrix(this Vector3 v, Vector3 origin = default(Vector3))
        {
            if (origin == default)
                origin = default;
            var radPitch = (Single)(v.X * Math.PI / 180f);
            var radYaw = (Single)(v.Y * Math.PI / 180f);
            var radRoll = (Single)(v.Z * Math.PI / 180f);

            var SP = (Single)Math.Sin(radPitch);
            var CP = (Single)Math.Cos(radPitch);
            var SY = (Single)Math.Sin(radYaw);
            var CY = (Single)Math.Cos(radYaw);
            var SR = (Single)Math.Sin(radRoll);
            var CR = (Single)Math.Cos(radRoll);

            var m = new Single[4, 4];
            m[0, 0] = CP * CY;
            m[0, 1] = CP * SY;
            m[0, 2] = SP;
            m[0, 3] = 0f;

            m[1, 0] = SR * SP * CY - CR * SY;
            m[1, 1] = SR * SP * SY + CR * CY;
            m[1, 2] = -SR * CP;
            m[1, 3] = 0f;

            m[2, 0] = -(CR * SP * CY + SR * SY);
            m[2, 1] = CY * SR - CR * SP * SY;
            m[2, 2] = CR * CP;
            m[2, 3] = 0f;

            m[3, 0] = origin.X;
            m[3, 1] = origin.Y;
            m[3, 2] = origin.Z;
            m[3, 3] = 1f;
            return m;
        }

        public static Vector3 CalcRotation(this Vector3 source, Vector3 destination, Vector3 origAngles, Single smooth)
        {
            var angles = new Vector3();
            var diff = source - destination;
            var hyp = Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
            angles.Y = (Single)Math.Atan(diff.Y / diff.X) * 57.295779513082f;
            angles.X = -(Single)Math.Atan(diff.Z / hyp) * 57.295779513082f;
            angles.Z = 0.0f;
            if (diff.X >= 0.0)
            {
                if (angles.Y > 0)
                    angles.Y -= 180.0f;
                else
                    angles.Y += 180.0f;
            }
            if (smooth > 0 && Math.Abs(angles.Y - origAngles.Y) < 180.0f)
                angles -= ((angles - origAngles) * smooth);
            return angles;
        }
        public static class Serializer
        {
            public static unsafe byte[] Serialize<T>(T value) where T : unmanaged
            {
                byte[] buffer = new byte[sizeof(T)];

                fixed (byte* bufferPtr = buffer)
                {
                    Buffer.MemoryCopy(&value, bufferPtr, sizeof(T), sizeof(T));
                }

                return buffer;
            }

            public static unsafe T Deserialize<T>(byte[] buffer, int offset = 0) where T : unmanaged
            {
                T result = new T();

                fixed (byte* bufferPtr = buffer)
                {
                    Buffer.MemoryCopy(bufferPtr + offset, &result, sizeof(T), sizeof(T));
                }

                return result;
            }
        }
        public struct Transform
        {
            public Vector3Double Rotation;
            public Double RotationW;
            public Vector3Double Translation;
            public Double TranslationW;
            public Vector3Double Scale;
            public Double ScaleW;
            public Matrix4x4 ToMatrixWithScale()
            {
                var x2 = Rotation.X + Rotation.X;
                var y2 = Rotation.Y + Rotation.Y;
                var z2 = Rotation.Z + Rotation.Z;

                var xx2 = Rotation.X * x2;
                var yy2 = Rotation.Y * y2;
                var zz2 = Rotation.Z * z2;

                var yz2 = Rotation.Y * z2;
                var wx2 = RotationW * x2;

                var xy2 = Rotation.X * y2;
                var wz2 = RotationW * z2;

                var xz2 = Rotation.X * z2;
                var wy2 = RotationW * y2;

                var m = new Matrix4x4
                {
                    M41 = (float)Translation.X,
                    M42 = (float)Translation.Y,
                    M43 = (float)Translation.Z,
                    M11 = (float)((1.0f - (yy2 + zz2)) * Scale.X),
                    M22 = (float)((1.0f - (xx2 + zz2)) * Scale.Y),
                    M33 = (float)((1.0f - (xx2 + yy2)) * Scale.Z),
                    M32 = (float)((yz2 - wx2) * Scale.Z),
                    M23 = (float)((yz2 + wx2) * Scale.Y),
                    M21 = (float)((xy2 - wz2) * Scale.Y),
                    M12 = (float)((xy2 + wz2) * Scale.X),
                    M31 = (float)((xz2 + wy2) * Scale.Z),
                    M13 = (float)((xz2 - wy2) * Scale.X),
                    M14 = 0.0f,
                    M24 = 0.0f,
                    M34 = 0.0f,
                    M44 = 1.0f
                };
                return m;
            }
            public Single[,] ToMatrixWithScale2()
            {
                var m = new Single[4, 4];

                m[3, 0] = (float)Translation.X;
                m[3, 1] = (float)Translation.Y;
                m[3, 2] = (float)Translation.Z;

                var x2 = Rotation.X * 2;
                var y2 = Rotation.Y * 2;
                var z2 = Rotation.Z * 2;

                var xx2 = Rotation.X * x2;
                var yy2 = Rotation.Y * y2;
                var zz2 = Rotation.Z * z2;
                m[0, 0] = (float)((1.0f - (yy2 + zz2)) * Scale.X);
                m[1, 1] = (float)((1.0f - (xx2 + zz2)) * Scale.Y);
                m[2, 2] = (float)((1.0f - (xx2 + yy2)) * Scale.Z);

                var yz2 = Rotation.Y * z2;
                var wx2 = RotationW * x2;
                m[2, 1] = (float)((yz2 - wx2) * Scale.Z);
                m[1, 2] = (float)((yz2 + wx2) * Scale.Y);

                var xy2 = Rotation.X * y2;
                var wz2 = RotationW * z2;
                m[1, 0] = (float)((xy2 - wz2) * Scale.Y);
                m[0, 1] = (float)((xy2 + wz2) * Scale.X);

                var xz2 = Rotation.X * z2;
                var wy2 = RotationW * y2;
                m[2, 0] = (float)((xz2 + wy2) * Scale.Z);
                m[0, 2] = (float)((xz2 - wy2) * Scale.X);

                m[0, 3] = 0.0f;
                m[1, 3] = 0.0f;
                m[2, 3] = 0.0f;
                m[3, 3] = 1.0f;

                return m;
            }
        }
        static Vector3 LastRotation = Vector3.Zero;
        static Vector3 vAxisX = Vector3.Zero;
        static Vector3 vAxisY = Vector3.Zero;
        static Vector3 vAxisZ = Vector3.Zero;
        public static Vector2 WorldToScreen(Vector3 worldLocation, Vector3 cameraLocation, Vector3 cameraRotation, Single fieldOfView, int ScreenCenterX, int ScreenCenterY)
        {
            if (LastRotation != cameraRotation)
            {
                cameraRotation.GetAxes(out vAxisX, out vAxisY, out vAxisZ);
                LastRotation = cameraRotation;
            }
            var vDelta = worldLocation - cameraLocation;
            var vTransformed = new Vector3(vDelta.Mult(vAxisY), vDelta.Mult(vAxisZ), vDelta.Mult(vAxisX));
            if (vTransformed.Z < 1f) vTransformed.Z = 1f;
            var fullScreen = new Vector2(ScreenCenterX + vTransformed.X * (ScreenCenterX / (float)Math.Tan(fieldOfView * (float)Math.PI / 360)) / vTransformed.Z,
                ScreenCenterY - vTransformed.Y * (ScreenCenterX / (float)Math.Tan(fieldOfView * (float)Math.PI / 360)) / vTransformed.Z);
            return new Vector2(fullScreen.X, fullScreen.Y);
        }
    }
}