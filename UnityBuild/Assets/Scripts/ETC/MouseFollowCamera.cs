using UnityEngine;

public class MouseFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform; // 🎯 카메라 트랜스폼
    [SerializeField] private float maxRotationAngle = 5f; // 🎯 최대 회전 각도 (기울기)
    [SerializeField] private float smoothSpeed = 5f; // 🎯 부드러운 이동 속도

    private Vector3 defaultRotation; // 기본 카메라 회전값 저장

    private void Start()
    {
        defaultRotation = cameraTransform.localRotation.eulerAngles; // 🎯 초기 회전값 저장
    }

    private void Update()
    {
        RotateCameraBasedOnMouse();
    }

    private void RotateCameraBasedOnMouse()
    {
        // 🎯 마우스 위치 가져오기 (화면 비율 기준 0~1로 변환)
        float mouseX = Input.mousePosition.x / Screen.width;
        float mouseY = Input.mousePosition.y / Screen.height;

        // 🎯 화면 중심에서의 이동량 계산 (-0.5 ~ 0.5 범위)
        float offsetX = (mouseX - 0.5f) * 2f; 
        float offsetY = (mouseY - 0.5f) * 2f; 

        // 🎯 카메라 회전 각도 계산 (최대 기울기 적용)
        float targetRotX = defaultRotation.x - (offsetY * maxRotationAngle); // 위아래 기울기
        float targetRotY = defaultRotation.y + (offsetX * maxRotationAngle); // 좌우 기울기

        // 🎯 현재 회전을 목표 회전으로 부드럽게 보간 (Lerp 사용)
        Vector3 targetRotation = new Vector3(targetRotX, targetRotY, defaultRotation.z);
        cameraTransform.localRotation = Quaternion.Lerp(
            cameraTransform.localRotation,
            Quaternion.Euler(targetRotation),
            Time.deltaTime * smoothSpeed
        );
    }
}