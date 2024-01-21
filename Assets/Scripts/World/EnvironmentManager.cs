
using UnityEngine;


public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Vector2 windVelocity;
    [SerializeField] private float windStrength;
    [SerializeField] private float windScale;

    private void Awake()
    {
        Instance = this;
    }

    public static Vector2 GetWind(Vector2 pos)
    {
        Vector3 noisePosX = new Vector3(pos.x + Instance.windVelocity.x * Time.time, pos.y + Instance.windVelocity.y * Time.time, 0.0f);
        Vector3 noisePosY = new Vector3(pos.x + Instance.windVelocity.x * Time.time, pos.y + Instance.windVelocity.y * Time.time, 5.0f);
        NoiseData.SimplexNoise3D_float(noisePosX, Instance.windScale, out float noiseX, out Vector3 _);
        NoiseData.SimplexNoise3D_float(noisePosY, Instance.windScale, out float noiseY, out Vector3 _);
        return new Vector2(noiseX, noiseY) * Instance.windStrength;
    }
}
