using UnityEngine;

public class PlayerBiomeDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMPro.TextMeshProUGUI biomeText;
    [SerializeField] private PlayerMovement PlayerMovement;

    [Header("Config")]
    [SerializeField] private float fadeDelay = 3f;
    [SerializeField] private float fadeDuration = 1f;

    private float timer;
    private Biome currentBiome;

    private void Update()
    {
        // Update current biome
        if (PlayerMovement.ClosestEdge == null) return;
        Biome currentPlayerBiome = PlayerMovement.ClosestEdge.biome;
        SetBiome(currentPlayerBiome);

        // Update timer
        timer -= Time.deltaTime;
        if (timer > fadeDuration)
        {
            Color color = biomeText.color;
            color.a = 1f;
            biomeText.color = color;
        }
        else if (timer < fadeDuration)
        {
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            Color color = biomeText.color;
            color.a = alpha;
            biomeText.color = color;
        }
    }

    private void SetBiome(Biome biome)
    {
        if (currentBiome == biome) return;
        currentBiome = biome;
        biomeText.text = biome.biomeName;
        timer = fadeDelay + fadeDuration;
    }
}
