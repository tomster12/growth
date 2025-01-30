using System.Collections.Generic;
using UnityEngine;

public class EnergyOrbManager : MonoBehaviour
{
    private const float GRAVITY_FORCE = 0.1f;
    private const float ORB_PUSH_FORCE = 0.1f;
    private const float ORB_PULL_FORCE = 0.00015f;
    private const float HOVER_FORCE = 0.075f;
    private const float UNSTUCK_FORCE = 5f;
    private const float MOUSE_FORCE = 200f;
    private const float WIND_FORCE = 0.025f;
    private const float MOUSE_MAX_FORCE = 0.2f;
    private const float VELOCITY_FORCE = 0.05f;
    private const float VELOCITY_MAX_FORCE = 0.2f;
    private const float FORCE_DAMPING = 0.985f;

    private const float ORB_PUSH_DIST = 0.5f;
    private const float ORB_PULL_DIST = 3.0f;
    private const float MOUSE_DIST = 2f;
    private const float VELOCITY_DIST = 2.5f;

    private const float HOVER_BASE_HEIGHT = 0.6f;
    private const float HOVER_SIZE_HEIGHT = 4.0f;
    private const float HOVER_GRAVITY_THRESHOLD = 0.15f;
    private const float HOVER_OSC_AMT = 0.3f;
    private const float HOVER_OSC_FREQ = 0.2f;

    private const int MAX_ORB_COUNT = 1024;

    [Header("References")]
    [SerializeField] private World world;
    [SerializeField] private Transform outputGO;
    [SerializeField] private SpriteRenderer outputSR;
    [SerializeField] private Material outputMaterialPfb;

    private Material outputMaterial;
    private ComputeBuffer orbDatasBuffer;
    private List<Vector4> orbDatas = new List<Vector4>();
    private List<Vector2> orbVels = new List<Vector2>();
    private int orbCount = 0;

    private void Start()
    {
        VelocitySource.OnVelocity += OnVelocity;

        // Init the orb data buffer
        orbDatasBuffer?.Release();
        orbDatasBuffer = new ComputeBuffer(MAX_ORB_COUNT, sizeof(float) * 4);

        // Update the output GO
        outputMaterial = new Material(outputMaterialPfb);
        outputGO.transform.position = GameLayers.OnLayer(world.GetCentre(), GameLayer.Terrain);
        outputGO.transform.localScale = Vector3.one * world.WorldGenerator.AtmosphereRadius * 2.0f;
        outputSR.material = outputMaterial;

        // Add some initial orbs
        for (int i = 0; i < 100; i++)
        {
            AddRandomOrb();
        }
    }

    private void OnDestroy()
    {
        // Clean up
        VelocitySource.OnVelocity -= OnVelocity;
        orbDatasBuffer?.Release();
        orbDatasBuffer = null;
    }

    private void Update()
    {
        Vector2 mousePos = PlayerController.Instance.MousePosition;
        Vector2 mouseChange = PlayerController.Instance.MouseChange * Time.deltaTime;

        for (int i = 0; i < orbCount; i++)
        {
            Vector4 orbData = orbDatas[i];
            Vector2 orbPos = new(orbData.x, orbData.y);

            // Find closest ground edge
            BVHEdge closestGroundEdge = world.TerrainBVH.FindClosestElement(orbPos);
            Vector2 closestGroundPos = closestGroundEdge.ClosestPoint(orbPos);
            Vector2 groundDir = closestGroundPos - orbPos;

            // Hover above the ground
            float signedDistance = closestGroundEdge.SignedDistance(orbPos);
            float hoverHeight = HOVER_BASE_HEIGHT + Mathf.Lerp(HOVER_SIZE_HEIGHT, 0.0f, orbData.z);
            hoverHeight += Mathf.Sin((Time.time + i * 0.168f) * HOVER_OSC_FREQ * Mathf.PI * 2) * HOVER_OSC_AMT;

            // Unstuck from the ground
            if (signedDistance < 0)
            {
                orbVels[i] = groundDir.normalized * UNSTUCK_FORCE;
                orbData.x = closestGroundPos.x;
                orbData.y = closestGroundPos.y;
            }

            // Push away from the ground
            else if (signedDistance < hoverHeight)
            {
                if (groundDir.sqrMagnitude < 0.1f)
                {
                    orbVels[i] = Vector2.zero;
                }
                orbVels[i] += -groundDir.normalized * HOVER_FORCE;
            }

            // Gravity force
            else
            {
                float amount = Mathf.Clamp01((signedDistance - hoverHeight) / HOVER_GRAVITY_THRESHOLD);
                Vector2 gravityDir = world.GetCentre() - orbPos;
                orbVels[i] += amount * GRAVITY_FORCE * gravityDir.normalized;
            }

            // Mouse interaction force
            float mouseDist = Vector2.Distance(mousePos, orbPos);
            if (mouseDist < MOUSE_DIST)
            {
                float magnitude = Mathf.Clamp(mouseChange.magnitude * MOUSE_FORCE, 0, MOUSE_MAX_FORCE);
                magnitude *= (1 - Mathf.Clamp01(mouseDist / MOUSE_DIST));
                orbVels[i] += magnitude * mouseChange.normalized;
            }

            // Wind force
            orbVels[i] += GlobalWind.GetWind(orbPos) * WIND_FORCE;

            for (int j = i + 1; j < orbCount; j++)
            {
                Vector2 otherOrbPos = new(orbDatas[j].x, orbDatas[j].y);
                Vector2 dir = otherOrbPos - orbPos;
                float dist = dir.magnitude;
                float amount = 0.0f;

                // Move away
                if (dist < ORB_PUSH_DIST)
                {
                    float pct = Mathf.Clamp01((ORB_PUSH_DIST - dist) / ORB_PUSH_DIST);
                    amount = pct * pct * ORB_PUSH_FORCE;
                }

                // Move towards
                else if (dist < ORB_PULL_DIST)
                {
                    amount = -ORB_PULL_FORCE;
                }

                orbVels[i] += -dir.normalized * amount;
                orbVels[j] += dir.normalized * amount;
            }

            // Apply velocity
            orbData.x += orbVels[i].x * Time.deltaTime;
            orbData.y += orbVels[i].y * Time.deltaTime;
            orbVels[i] *= FORCE_DAMPING;

            orbDatas[i] = orbData;
        }

        UpdateOutput();
    }

    private void UpdateOutput()
    {
        if (orbCount == 0) return;
        if (orbCount > MAX_ORB_COUNT) throw new System.Exception("Too many orbs!");

        orbDatasBuffer.SetData(orbDatas);
        outputMaterial.SetBuffer("_OrbData", orbDatasBuffer);
        outputMaterial.SetInt("_OrbCount", orbCount);
    }

    private void AddRandomOrb()
    {
        Vector2 position = new Vector2(15.0f + Random.Range(-5f, 5f), 85.0f + Random.Range(-5f, 5f));
        float size = Random.Range(0.2f, 1.0f);
        AddOrb(position, size);
    }

    private void AddOrb(Vector2 position, float size)
    {
        orbDatas.Add(new Vector4(position.x, position.y, size, 0.0f));
        orbVels.Add(Vector4.zero);
        orbCount++;
    }

    private void OnVelocity(Vector2 position, Vector2 velocity)
    {
        for (int i = 0; i < orbCount; i++)
        {
            Vector2 dir = new Vector2(orbDatas[i].x - position.x, orbDatas[i].y - position.y);
            float dist = dir.magnitude;
            if (dist < VELOCITY_DIST)
            {
                float amount = Mathf.Clamp01((VELOCITY_DIST - dist) / VELOCITY_DIST);
                orbVels[i] += velocity * amount * VELOCITY_FORCE;
            }
        }
    }
}
