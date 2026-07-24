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
        /// Applies the physics tunables to the car. Called at spawn and again whenever the user
        /// edits a setting in the UMM window, so mass / COM / tilt freeze are live experiments.
        /// </summary>
        public void ApplyPhysicsSettings(Settings settings)
        {
            var rb = Car.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            if (settings.LetGameOwnPhysics)
            {
                // Hands off. The game's TrainMassController and per-bogie suspension already
                // sit a flatcar correctly on the rails. Overwriting rb.mass here desynced the
                // bogie joint springs (tuned by the game for the real mass at spawn) and sank
                // the wheels into the rail. Aligned with the direction: behave like a vanilla
                // car by letting the game own the simulation.
                return;
            }

            // Legacy placeholder path, kept for A/B. Bypasses TrainCar.massController, which
            // normally distributes mass between the body and the bogies.
            rb.mass = settings.Mass;
            rb.centerOfMass = new Vector3(0f, settings.ComHeight, 0f);

            // The tilt freeze is a stand-in for real suspension: it prevents roll-over, and it
            // is also why the car cannot derail.
            rb.constraints = settings.FreezeCarTilt
                ? RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ
                : RigidbodyConstraints.None;

            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // The bogies are separate rigidbodies. With the body interpolated and the bogies
            // not, the now-visible wheels stutter relative to the body in first person.
            ApplyBogieInterpolation(settings.SmoothBogies);
        }

        private void ApplyBogieInterpolation(bool smooth)
        {
            Bogie[] bogies = Car.Bogies;
            if (bogies == null)
                return;

            for (int i = 0; i < bogies.Length; i++)
            {
                Bogie bogie = bogies[i];
                if (bogie == null || bogie.rb == null)
                    continue;

                bogie.rb.interpolation = smooth
                    ? RigidbodyInterpolation.Interpolate
                    : RigidbodyInterpolation.None;
            }
        }

        /// <summary>
        /// Applies propulsion, subject to the speed cap. Positive force drives toward the car's
        /// forward, negative reverses.
        ///
        /// Preferred path is the game's own: <c>Bogie.ApplyForce</c> pushes each bogie's
        /// rigidbody along the *bogie's* forward, which follows the rail. Pushing the carbody
        /// along the *car's* forward (the fallback) diverges from the rail on every curve and
        /// fights the bogie joints — the visible jitter.
        /// </summary>
        public void ApplyForwardForce(float force, float maxSpeed, bool viaBogies)
        {
            var rb = Car.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            if (viaBogies && Car.AreBogiesFullyInitialized())
            {
                Bogie[] bogies = Car.Bogies;
                if (bogies != null && bogies.Length > 0)
                {
                    ApplyForceViaBogies(bogies, force, maxSpeed);
                    return;
                }
            }

            // Fallback: push the carbody directly (pre-bogie behaviour).
            float forwardSpeed = Vector3.Dot(rb.velocity, Car.transform.forward);
            if (!PropulsionPolicy.ShouldApplyForce(force, forwardSpeed, maxSpeed))
                return;

            rb.AddForce(Car.transform.forward * force, ForceMode.Force);
        }

        private void ApplyForceViaBogies(Bogie[] bogies, float force, float maxSpeed)
        {
            float perBogie = force / bogies.Length;

            for (int i = 0; i < bogies.Length; i++)
            {
                Bogie bogie = bogies[i];
                if (bogie == null || bogie.rb == null)
                    continue;

                // A derailed bogie is off the rail: pushing it along its forward is a
                // free-space rocket, not traction.
                if (bogie.HasDerailed)
                    continue;

                // Bogie forward is not guaranteed to agree with car forward, so align the
                // sign per bogie — otherwise a flipped bogie would brake instead of drive.
                float sign = Mathf.Sign(Vector3.Dot(bogie.transform.forward, Car.transform.forward));
                float bogieForce = perBogie * sign;

                // Gate on the bogie's OWN velocity, in its own frame. Gating on the car body
                // once let a decoupled bogie be pumped to ~6000 m/s by a held key (see
                // Player.log "Point Set Traveller not moving even though velocity is: 5963"),
                // which ended in a heightmap overflow crash.
                float bogieSpeed = Vector3.Dot(bogie.rb.velocity, bogie.transform.forward);
                if (!PropulsionPolicy.ShouldApplyForce(bogieForce, bogieSpeed, maxSpeed))
                    continue;

                bogie.ApplyForce(bogieForce);
            }
        }
    }
}
