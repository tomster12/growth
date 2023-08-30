
using UnityEngine;


public class PluckableStone : WorldObject
{
    [Header("Pluck Config")]
    [SerializeField] private float pluckTimerMax = 1.0f;
    [SerializeField] private float pluckVelocity = 30.0f;
    [SerializeField] private GameObject pluckPsysPfb;

    private InteractionPluck interactionPluck;
    [HideInInspector] public Vector2 popDir;


    protected void Awake()
    {
        InitComponentHighlight();
        InitComponentControl();
    }

    protected void Start()
    {
        SetCanControl(false);
        interactionPluck = new InteractionPluck(OnPluck, pluckTimerMax);
        interactions.Add(interactionPluck);
    }


    private void Update()
    {
        interactionPluck.Update();

        float angle = 270.0f + (-45.0f + Mathf.Sin((Time.time / (2.0f * Mathf.PI)) * 0.5f) * 90.0f);
        // Debug.Log(angle);
        SetControlAngle(angle);
    }

    private void OnPluck()
    {
        // Become physical and controllable
        InitComponentPhysical();
        SetCanControl(true);

        // Move out of ground
        RB.transform.position += (Vector3)(popDir.normalized * cl.bounds.extents * 1.5f);

        // Pop in a direction (ignore mass)
        RB.AddForce(popDir.normalized * pluckVelocity * RB.mass, ForceMode2D.Impulse);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPsysPfb);
        particlesGO.transform.position = RB.transform.position;
        particlesGO.transform.up = popDir.normalized;
    }
}
