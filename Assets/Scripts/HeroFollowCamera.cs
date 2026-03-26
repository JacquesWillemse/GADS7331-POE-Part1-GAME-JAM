using UnityEngine;
using UnityEngine.InputSystem;

public class HeroFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform heroTarget;

    [Header("Camera Anchors (Up, Right, Down, Left)")]
    [SerializeField] private Transform[] cameraAnchors = new Transform[4];

    [Header("Follow")]
    [SerializeField] private float positionLerpSpeed = 8f;
    [SerializeField] private float lookAtHeightOffset = 1f;

    private int _currentViewIndex;

    private void LateUpdate()
    {
        if (heroTarget == null || cameraAnchors == null || cameraAnchors.Length == 0)
        {
            return;
        }

        HandleInput();

        Transform currentAnchor = GetCurrentAnchor();
        if (currentAnchor == null)
        {
            return;
        }

        Vector3 desiredPosition = currentAnchor.position;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionLerpSpeed * Time.deltaTime);

        Vector3 lookPoint = heroTarget.position + Vector3.up * lookAtHeightOffset;
        transform.LookAt(lookPoint);
    }

    private void HandleInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            _currentViewIndex = (_currentViewIndex + 1) % cameraAnchors.Length;
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            _currentViewIndex = (_currentViewIndex - 1 + cameraAnchors.Length) % cameraAnchors.Length;
        }
    }

    public void SetHeroTarget(Transform target)
    {
        heroTarget = target;
    }

    private Transform GetCurrentAnchor()
    {
        if (_currentViewIndex < 0 || _currentViewIndex >= cameraAnchors.Length)
        {
            return null;
        }

        return cameraAnchors[_currentViewIndex];
    }
}
