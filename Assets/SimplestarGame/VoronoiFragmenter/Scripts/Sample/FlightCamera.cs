using UnityEngine;

namespace SimplestarGame
{
    public class FlightCamera : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 10f; // 移動速度
        [SerializeField] float lookSpeed = 5f; // 視点の回転速度
        [SerializeField] TouchInput touchInput;

        void Start()
        {
            this.rotation = this.transform.localRotation.eulerAngles;
            if (null != this.touchInput)
            {
                this.touchInput.onLeftAxis += this.OnLeftAxis;
                this.touchInput.onRightAxis += this.OnRightAxis;
            }
        }

        void OnLeftAxis(Vector2 axis)
        {
            this.MoveCamera(axis.x, axis.y);
        }

        void OnRightAxis(Vector2 axis)
        {
            this.RotateCamera(axis.x, axis.y);
        }

        void Update()
        {
            if (Application.isMobilePlatform)
            {
                return;
            }
            if (Input.GetMouseButton(1))
            {
                // マウスの移動量を取得して、回転値に加算
                this.RotateCamera(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            }

            // WASDキーで移動
            this.MoveCamera(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        void MoveCamera(float axisX, float axisY)
        {
            // Q,Eでエレベーター移動
            float up = Input.GetKey(KeyCode.E) ? 1f : 0f;
            float down = Input.GetKey(KeyCode.Q) ? -1f : 0f;
            Vector3 moveDir = new Vector3(axisX, up + down, axisY);
            this.transform.position += this.transform.TransformDirection(moveDir) * this.moveSpeed * Time.deltaTime;
        }

        void RotateCamera(float axisX, float axisY)
        {
            this.rotation.y += axisX * this.lookSpeed;
            this.rotation.x += axisY * this.lookSpeed;

            // 回転値をクランプし、カメラの回転を設定
            this.rotation.x = Mathf.SmoothDamp(this.rotation.x, Mathf.Clamp(this.rotation.x, -90f, 90f), ref this.velocity, 1f);
            this.transform.localRotation = Quaternion.Euler(this.rotation.x, this.rotation.y, 0f);
        }

        float velocity;
        Vector2 rotation = Vector2.zero; // 視点の回転値
    }
}