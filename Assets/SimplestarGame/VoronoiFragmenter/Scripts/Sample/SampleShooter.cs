using System.Collections;
using UnityEngine;

namespace SimplestarGame
{
    public class SampleShooter : MonoBehaviour
    {
        [SerializeField] float explosionForce = 300f;
        [SerializeField] float explosionRadius = 1f;
        [SerializeField] AudioClip[] audioClips;
        [SerializeField] AudioSource[] audioSources;
        [SerializeField] Camera mainCamera;
        [SerializeField] TouchInput touchInput;
        [SerializeField] SampleShootType shootType;

        internal System.Action<Ray> onShoot;

        void Start()
        {
            if (null != this.touchInput)
            {
                this.touchInput.onLeftTap += this.OnLeftTap;
                this.touchInput.onRightTap += this.OnRightTap;
            }
        }

        void OnLeftTap(Vector2 point)
        {
            this.FragmentObject(point);
        }

        void OnRightTap(Vector2 point)
        {
            this.FragmentObject(point);
        }

        void Update()
        {
            if (Application.isMobilePlatform)
            {
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 point = Input.mousePosition;
                this.FragmentObject(point);
            }
        }

        void FragmentObject(Vector2 point)
        {
            var ray = this.mainCamera.ScreenPointToRay(point);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                switch (this.shootType)
                {
                    case SampleShootType.Bullet:
                        {
                            if (hit.collider.gameObject.TryGetComponent(out VoronoiFragmenter voronoiFragment))
                            {
                                if (!hit.collider.gameObject.TryGetComponent(out Rigidbody rigidbody))
                                {
                                    rigidbody = hit.collider.gameObject.AddComponent<Rigidbody>();
                                }
                                rigidbody.isKinematic = false;
                                this.onShoot?.Invoke(ray);
                            }
                        }
                        break;
                    case SampleShootType.Direct:
                    default:
                        {
                            float scale = 3f;
                            if (hit.collider.gameObject.TryGetComponent(out VoronoiFragmenter voronoiFragment))
                            {
                                scale = 1f;
                                voronoiFragment.Fragment(hit);
                            }
                            this.PlaySE(hit);

                            StartCoroutine(this.CoExplodeObjects(hit, scale));
                        }
                        break;
                }
            }

        }

        void PlaySE(RaycastHit hit)
        {
            if (null != this.audioSources && null != this.audioClips &&
                0 < this.audioClips.Length && 0 < this.audioSources.Length)
            {
                var audioSource = this.audioSources[this.nextAudioSourceIndex];
                this.nextAudioSourceIndex++;
                if (this.audioSources.Length == this.nextAudioSourceIndex)
                {
                    this.nextAudioSourceIndex = 0;
                }
                audioSource.clip = this.audioClips[Random.Range(0, this.audioClips.Length)];
                audioSource.transform.position = hit.point;
                audioSource.Play();
            }
        }

        IEnumerator CoExplodeObjects(RaycastHit hit, float scale)
        {
            yield return new WaitForFixedUpdate();
            Collider[] colliders = Physics.OverlapSphere(hit.point, this.explosionRadius);
            foreach (var item in colliders)
            {
                if (item.TryGetComponent(out Rigidbody rigidbody))
                {
                    rigidbody.isKinematic = false;
                    rigidbody.AddExplosionForce(this.explosionForce * scale, hit.point + hit.normal * 0.1f, this.explosionRadius * scale);
                }
            }
        }


        int nextAudioSourceIndex = 0;
    }

    public enum SampleShootType
    {
        Direct = 0,
        Bullet,
        Max
    }
}