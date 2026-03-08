using UnityEngine;

namespace LastDay.Effects
{
    /// <summary>
    /// Configures and drives a Unity ParticleSystem to simulate floating dust motes
    /// drifting through the scene. Add this component to any empty GameObject; it
    /// creates and configures the ParticleSystem automatically in Awake — no manual
    /// setup required.
    ///
    /// Suggested placement: child of the Environment GameObject, Z = 0.
    /// Sorting layer should be set to a layer between the background and UI.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class AmbientDustEffect : MonoBehaviour
    {
        [Header("Particle Count & Lifetime")]
        [SerializeField, Range(10, 100)] private int maxParticles = 35;
        [SerializeField, Range(4f, 20f)]  private float lifetimeMin = 7f;
        [SerializeField, Range(4f, 20f)]  private float lifetimeMax = 14f;

        [Header("Movement")]
        [SerializeField, Range(0f, 0.05f)] private float driftUpSpeed = 0.012f;
        [SerializeField, Range(0f, 0.05f)] private float horizontalWobble = 0.018f;

        [Header("Size")]
        [SerializeField, Range(0.005f, 0.1f)] private float sizeMin = 0.012f;
        [SerializeField, Range(0.005f, 0.1f)] private float sizeMax = 0.04f;

        [Header("Appearance")]
        [SerializeField] private Color dustColor = new Color(1f, 0.97f, 0.88f, 0.12f);

        [Header("Spawn Area")]
        [SerializeField] private Vector2 spawnBoxSize = new Vector2(16f, 9f);

        [Header("Sorting")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 5;

        private ParticleSystem _ps;
        private ParticleSystemRenderer _psRenderer;

        void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            _psRenderer = GetComponent<ParticleSystemRenderer>();
            ConfigureSystem();
        }

        private void ConfigureSystem()
        {
            // Main module
            var main = _ps.main;
            main.loop              = true;
            main.startLifetime     = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
            main.startSpeed        = 0f;
            main.startSize         = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor        = dustColor;
            main.maxParticles      = maxParticles;
            main.simulationSpace   = ParticleSystemSimulationSpace.World;
            main.playOnAwake       = true;

            // Emission — constant trickle
            var emission = _ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = maxParticles / ((lifetimeMin + lifetimeMax) * 0.5f);

            // Shape — flat box covering scene width
            var shape = _ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(spawnBoxSize.x, spawnBoxSize.y, 0.01f);

            // Velocity over lifetime — slow upward drift with horizontal wobble
            var vol = _ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space   = ParticleSystemSimulationSpace.World;
            vol.x       = new ParticleSystem.MinMaxCurve(-horizontalWobble, horizontalWobble);
            vol.y       = new ParticleSystem.MinMaxCurve(driftUpSpeed * 0.5f, driftUpSpeed);
            vol.z       = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Fade in and out over lifetime
            var colorOverLifetime = _ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(dustColor.r, dustColor.g, dustColor.b), 0f),
                    new GradientColorKey(new Color(dustColor.r, dustColor.g, dustColor.b), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f,              0f),
                    new GradientAlphaKey(dustColor.a,     0.15f),
                    new GradientAlphaKey(dustColor.a,     0.80f),
                    new GradientAlphaKey(0f,              1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            // Size variation over lifetime — slight grow then shrink
            var sizeOverLifetime = _ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f,   0.3f);
            sizeCurve.AddKey(0.2f, 1.0f);
            sizeCurve.AddKey(0.8f, 1.0f);
            sizeCurve.AddKey(1f,   0.3f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renderer — assign a built-in alpha-blended material so particles
            // render as soft white dots instead of magenta error squares
            _psRenderer.renderMode       = ParticleSystemRenderMode.Billboard;
            _psRenderer.sortingLayerName = sortingLayerName;
            _psRenderer.sortingOrder     = sortingOrder;

            if (_psRenderer.sharedMaterial == null
                || _psRenderer.sharedMaterial.name.Contains("Default-Particle") == false)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader != null)
                    _psRenderer.sharedMaterial = new Material(shader);
            }

            _ps.Play();
        }

#if UNITY_EDITOR
        // Re-apply config when Inspector values change in Edit Mode
        void OnValidate()
        {
            if (_ps == null) _ps = GetComponent<ParticleSystem>();
            if (_psRenderer == null) _psRenderer = GetComponent<ParticleSystemRenderer>();
            if (_ps != null) ConfigureSystem();
        }
#endif
    }
}
