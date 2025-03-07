using UnityEngine;

namespace Player
{
    public partial class PlayerCharacter
    {
        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float cameraOffset = 5f;
        public float maxCameraDistance = 5f;

        [SerializeField] private GameObject playerModel;

        private void UpdateCameraTarget()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, mouseTargetLayer))
            {
                Vector3 mousePosition = hitInfo.point;
                Vector3 playerPosition = playerModel.transform.position;

                Vector3 targetPosition = Vector3.Lerp(playerPosition, mousePosition, 0.5f);
                targetPosition.y = playerPosition.y;

                Vector3 offset = targetPosition - playerPosition;
                if (offset.magnitude > maxCameraDistance)
                {
                    offset = offset.normalized * maxCameraDistance;
                }

                Vector3 limitedTargetPosition = playerPosition + offset;

                CinemachineCameraTarget.transform.position = Vector3.Lerp(
                    CinemachineCameraTarget.transform.position,
                    limitedTargetPosition,
                    Time.deltaTime * cameraOffset
                );
            }
        }
    }
}