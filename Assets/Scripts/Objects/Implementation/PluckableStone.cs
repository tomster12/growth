
using UnityEngine;


public class PluckableStone : ComposableObject
{
    [Header("Pluck Config")]
    [SerializeField] private float density = 2.0f;
    [SerializeField] private float pluckTimerMax = 1.0f;
    [SerializeField] private float pluckVelocity = 30.0f;
    [SerializeField] private GameObject pluckPsysPfb;

    public Vector2 popDir;
    
    private InteractionPluck interactionPluck;


    protected void Awake()
    {
        AddPartInteractable();
        AddPartHighlightable();
    }

    protected void Start()
    {
        interactionPluck = new InteractionPluck(OnPluck, pluckTimerMax);
        PartInteractable.Interactions.Add(interactionPluck);
    }


    private void Update()
    {
        interactionPluck.Update();
    }

    private void OnPluck()
    {
        // Become physical and controllable
        AddPartPhysical();
        AddPartControllable();
        PartControllable.SetCanControl(true);
        PartPhysical.InitMass(density);
        
        // Set popDir
        if (popDir.Equals(Vector2.zero))
        {
            Debug.LogWarning("PluckableStone does not have a popDir");
            popDir  = (Position - (Vector2)World.GetClosestWorld(Position).GetCentre()).normalized;
        }

        // Move out of ground
        PartPhysical.RB.transform.position += (Vector3)(popDir.normalized * CL.bounds.extents * 1.5f);

        // Pop in a direction (ignore mass)
        PartPhysical.RB.AddForce(popDir.normalized * pluckVelocity * PartPhysical.RB.mass, ForceMode2D.Impulse);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPsysPfb);
        particlesGO.transform.position = PartPhysical.RB.transform.position;
        particlesGO.transform.up = popDir.normalized;
    }
}
