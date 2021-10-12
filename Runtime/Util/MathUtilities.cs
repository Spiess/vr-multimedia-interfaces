using UnityEngine;

namespace Util
{
    internal static class MathUtilities
    {
        /// <summary>
        /// Convert a vector with 2D cartesian coordinates to a new vector with polar coordinates.
        /// </summary>
        /// <param name="cartesian">A vector with the 2D cartesian coordinates to be converted.</param>
        /// <returns>
        /// The converted polar coordinates as a new vector. The radius is stored in the <c>x</c> component and the
        /// angle in the <c>y</c> component of the vector.
        /// </returns>
        public static Vector2 CartesianToPolar(Vector2 cartesian)
        {
            return new Vector2(Mathf.Sqrt(cartesian.x * cartesian.x + cartesian.y * cartesian.y),
                Mathf.Atan2(cartesian.x, cartesian.y));
        }

        /// <summary>
        /// Convert a vector with polar coordinates to a new vector with 2D cartesian coordinates.
        /// </summary>
        /// <param name="polar">
        /// A vector with the polar coordinates to be converted. The radius is expected in the <c>x</c> component and
        /// the angle in the <c>y</c> component of the vector.
        /// </param>
        /// <returns>The converted 2D cartesian coordinates as a new 2d vector.</returns>
        public static Vector2 PolarToCartesian(Vector2 polar)
        {
            return new Vector2(polar.x * Mathf.Cos(polar.y), polar.x * Mathf.Sin(polar.y));
        }
        
        /// <summary>
        /// Convert a vector with 3D cartesian coordinates to a new vector with spherical coordinates.
        /// </summary>
        /// <param name="cartesian">A vector with the 3D cartesian coordinates to be converted.</param>
        /// <returns>
        /// The converted spherical coordinates as a new vector. The radius is stored in the <c>x</c> component and the
        /// angles in the <c>y</c> and <c>z</c> component of the vector (r, θ, φ).
        /// </returns>
        public static Vector3 CartesianToSpherical(Vector3 cartesian)
        {
            var r = Mathf.Sqrt(cartesian.x * cartesian.x + cartesian.y * cartesian.y + cartesian.z * cartesian.z);
            return new Vector3(r, Mathf.Acos(cartesian.y / r), Mathf.Atan2(-cartesian.z, cartesian.x));
        }

        /// <summary>
        /// Convert a vector with spherical coordinates to a new vector with 3D cartesian coordinates.
        /// </summary>
        /// <param name="spherical">
        /// A vector with the spherical coordinates to be converted. The radius is expected in the <c>x</c> component
        /// and the angles in the <c>y</c> and <c>z</c> component of the vector (r, θ, φ).
        /// </param>
        /// <returns>The converted 3D cartesian coordinates as a new vector.</returns>
        public static Vector3 SphericalToCartesian(Vector3 spherical)
        {
            return new Vector3(spherical.x * Mathf.Sin(spherical.y) * Mathf.Cos(spherical.z),
                spherical.x * Mathf.Cos(spherical.y), -spherical.x * Mathf.Sin(spherical.y) * Mathf.Sin(spherical.z));
        }

        /// <summary>
        /// Convert an angle in radians to an angle in degrees.
        /// </summary>
        /// <param name="radian">The angle in radians from 0 to 2π.</param>
        /// <returns>The angle in degrees from 0 to 360.</returns>
        public static float RadianToDegrees(float radian)
        {
            return radian / Mathf.PI * 180; // = radian / 2 / pi * 360
        }

        /// <summary>
        /// Convert an angle in degrees to an angle in radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees from 0 to 360.</param>
        /// <returns>The angle in radians from 0 to 2π.</returns>
        public static float DegreesToRadians(float degrees)
        {
            return degrees / 180 * Mathf.PI; // = degrees / 360 * 2 * pi
        }
    }
}