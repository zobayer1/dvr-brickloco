using BrickLoco.Logic;
using UnityEngine;

namespace BrickLoco.Game
{
    /// <summary>
    /// A spawned Derail Valley car that the mod has taken over: retuned physics, placeholder
    /// visuals, and a seat transform to mount to.
    /// </summary>
    internal class BrickCar
    {
        /// <summary>
        /// Seat offset from the car origin. Kept high enough to clear typical carbody colliders,
        /// so the player does not start a mount already penetrating geometry.
        /// </summary>
        public static readonly Vector3 SeatLocalPosition = new Vector3(0f, 2.5f, 0f);

        public TrainCar Car { get; private set; }

        /// <summary>Empty transform above the car that the player is parented to when mounted.</summary>
        public Transform Seat { get; private set; }

        public Transform Transform { get { return Car.transform; } }
        public string Name { get { return Car.name; } }

        /// <summary>True once the underlying car has been destroyed by the game.</summary>
        public bool IsGone { get { return Car == null; } }

        public BrickCar(TrainCar car, Transform seat)
        {
            Car = car;
            Seat = seat;
        }

        /// <summary>
        /// Applies propulsion along the car's forward axis, subject to the speed cap.
        /// Positive force drives forward, negative reverses.
        /// </summary>
        public void ApplyForwardForce(float force, float maxSpeed)
        {
            var rb = Car.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            float forwardSpeed = Vector3.Dot(rb.velocity, Car.transform.forward);

            if (!PropulsionPolicy.ShouldApplyForce(force, forwardSpeed, maxSpeed))
                return;

            rb.AddForce(Car.transform.forward * force, ForceMode.Force);
        }
    }
}
