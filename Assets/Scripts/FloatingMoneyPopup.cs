using TMPro;
using UnityEngine;

public class FloatingMoneyPopup : MonoBehaviour
{
    [SerializeField] private float lifetimeSeconds = 0.8f;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Color popupColor = new Color(0.95f, 0.9f, 0.25f, 1f);

    private TextMeshPro _text;
    private float _age;
    private Camera _mainCamera;

    public static void Spawn(Vector3 worldPosition, int amount)
    {
        GameObject popup = new GameObject("FloatingMoneyPopup");
        FloatingMoneyPopup popupComp = popup.AddComponent<FloatingMoneyPopup>();
        popupComp.Initialize(worldPosition, amount);
    }

    private void Initialize(Vector3 worldPosition, int amount)
    {
        transform.position = worldPosition + spawnOffset;
        _mainCamera = Camera.main;

        _text = gameObject.AddComponent<TextMeshPro>();
        _text.text = $"+{amount}";
        _text.fontSize = 4f;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = popupColor;
        _text.outlineWidth = 0.18f;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

        if (_mainCamera != null)
        {
            transform.forward = _mainCamera.transform.forward;
        }

        if (_text != null)
        {
            float t = Mathf.Clamp01(_age / lifetimeSeconds);
            Color c = popupColor;
            c.a = 1f - t;
            _text.color = c;
        }

        if (_age >= lifetimeSeconds)
        {
            Destroy(gameObject);
        }
    }
}
