using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    public float currentHealth;

    [Header("Level")]
    public int level = 1;

    [Header("Hit Reaction")]
    public float flinchDuration = 0.18f;
    public float flinchScaleAmount = 0.12f;

    [Header("Events / Behaviour")]
    public bool destroyOnDeath = true;
    public float destroyDelay = 1.5f;

    [Header("Health Bar (auto-generated, no setup needed)")]
    public bool showHealthBar = true;
    public Vector3 healthBarWorldOffset = new Vector3(0f, 2.2f, 0f);
    public Vector2 healthBarSize = new Vector2(1.4f, 0.16f);
    public Color barBackgroundColor = new Color(0f, 0f, 0f, 0.65f);
    public Color barFillColor = new Color(0.85f, 0.22f, 0.22f);
    public Color barFillLowColor = new Color(0.45f, 0.05f, 0.08f);
    public Color levelTextColor = Color.white;

    private bool isDead;
    private Canvas barCanvas;
    private Image fillImage;
    private Image flashImage;
    private Text levelText;
    private float flashAlpha;

    private Rigidbody rb;
    private Vector3 originalScale;
    private float flinchTimer;

    private Vector3 knockbackDir;
    private float knockbackRemaining;
    private float knockbackSpeed;

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;

        if (showHealthBar)
        {
            BuildHealthBar();
        }
    }

    void Update()
    {
        HandleKnockbackMovement();
        HandleFlinch();
    }

    void LateUpdate()
    {
        if (!showHealthBar || barCanvas == null) return;

        barCanvas.transform.position = transform.position + healthBarWorldOffset;

        Camera cam = Camera.main;
        if (cam != null)
        {
            barCanvas.transform.rotation = cam.transform.rotation;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv. {level}";
        }

        if (flashAlpha > 0f)
        {
            flashAlpha -= Time.deltaTime * 2.5f;

            if (flashImage != null)
            {
                Color c = flashImage.color;
                c.a = Mathf.Clamp01(flashAlpha);
                flashImage.color = c;
            }
        }
    }

    public void TakeDamage(float amount, GameObject attacker = null)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"{gameObject.name} HP: {currentHealth}/{maxHealth}");

        UpdateHealthBarVisual();
        flashAlpha = 1f;
        flinchTimer = flinchDuration;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void ApplyKnockback(Vector3 direction, float distance, float duration = 0.15f)
    {
        if (isDead) return;

        knockbackDir = direction.normalized;
        knockbackRemaining = distance;
        knockbackSpeed = duration > 0f ? distance / duration : distance;
    }

    private void HandleKnockbackMovement()
    {
        if (knockbackRemaining <= 0f) return;

        float step = Mathf.Min(
            knockbackSpeed * Time.deltaTime,
            knockbackRemaining
        );

        Vector3 delta = knockbackDir * step;

        if (rb != null)
        {
            rb.MovePosition(rb.position + delta);
        }
        else
        {
            transform.position += delta;
        }

        knockbackRemaining -= step;
    }

    private void HandleFlinch()
    {
        if (flinchTimer <= 0f)
        {
            transform.localScale = originalScale;
            return;
        }

        flinchTimer -= Time.deltaTime;

        float progress = 1f - Mathf.Clamp01(
            flinchTimer / flinchDuration
        );

        float curve = Mathf.Sin(progress * Mathf.PI);

        Vector3 squashed = new Vector3(
            originalScale.x * (1f + flinchScaleAmount * curve),
            originalScale.y * (1f - flinchScaleAmount * curve),
            originalScale.z * (1f + flinchScaleAmount * curve)
        );

        transform.localScale = squashed;
    }

    private void UpdateHealthBarVisual()
    {
        if (fillImage == null) return;

        float pct = maxHealth > 0f
            ? currentHealth / maxHealth
            : 0f;

        fillImage.fillAmount = pct;
        fillImage.color = Color.Lerp(
            barFillLowColor,
            barFillColor,
            pct
        );
    }

    private void Die()
    {
        isDead = true;

        Debug.Log($"{gameObject.name} died.");

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    public bool IsDead => isDead;

    void OnDestroy()
    {
        if (barCanvas != null)
        {
            Destroy(barCanvas.gameObject);
        }
    }

    private void BuildHealthBar()
    {
        const float refWidth = 200f;
        const float refHeight = 24f;

        GameObject canvasGO =
            new GameObject(gameObject.name + "_HealthBar");

        barCanvas = canvasGO.AddComponent<Canvas>();
        barCanvas.renderMode = RenderMode.WorldSpace;

        canvasGO.AddComponent<CanvasScaler>();

        RectTransform canvasRect =
            canvasGO.GetComponent<RectTransform>();

        canvasRect.sizeDelta =
            new Vector2(refWidth, refHeight);

        float scaleX = healthBarSize.x / refWidth;
        float scaleY = healthBarSize.y / refHeight;

        canvasGO.transform.localScale =
            new Vector3(scaleX, scaleY, 1f);

        GameObject bgGO =
            new GameObject("Background");

        bgGO.transform.SetParent(
            canvasGO.transform,
            false
        );

        Image bgImage =
            bgGO.AddComponent<Image>();

        bgImage.color = barBackgroundColor;
        bgImage.raycastTarget = false;

        RectTransform bgRect =
            bgGO.GetComponent<RectTransform>();

        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillGO =
            new GameObject("Fill");

        fillGO.transform.SetParent(
            canvasGO.transform,
            false
        );

        fillImage =
            fillGO.AddComponent<Image>();

        fillImage.color = barFillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;
        fillImage.raycastTarget = false;

        RectTransform fillRect =
            fillGO.GetComponent<RectTransform>();

        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        GameObject flashGO =
            new GameObject("Flash");

        flashGO.transform.SetParent(
            canvasGO.transform,
            false
        );

        flashImage =
            flashGO.AddComponent<Image>();

        flashImage.color =
            new Color(1f, 1f, 1f, 0f);

        flashImage.raycastTarget = false;

        RectTransform flashRect =
            flashGO.GetComponent<RectTransform>();

        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        GameObject levelGO =
            new GameObject("LevelText");

        levelGO.transform.SetParent(
            canvasGO.transform,
            false
        );

        levelText =
            levelGO.AddComponent<Text>();

        levelText.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        levelText.fontSize = 16;
        levelText.fontStyle = FontStyle.Bold;
        levelText.alignment = TextAnchor.MiddleRight;
        levelText.color = levelTextColor;
        levelText.text = $"Lv. {level}";
        levelText.raycastTarget = false;

        RectTransform levelRect =
            levelGO.GetComponent<RectTransform>();

        levelRect.anchorMin =
            new Vector2(0f, 0f);

        levelRect.anchorMax =
            new Vector2(0f, 1f);

        levelRect.pivot =
            new Vector2(1f, 0.5f);

        levelRect.sizeDelta =
            new Vector2(50f, refHeight);

        levelRect.anchoredPosition =
            new Vector2(-6f, 0f);

        UpdateHealthBarVisual();
    }
}