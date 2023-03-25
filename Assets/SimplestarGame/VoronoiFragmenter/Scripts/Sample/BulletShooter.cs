using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SimplestarGame
{
    public class BulletShooter : MonoBehaviour
    {
        [SerializeField] SampleShooter shooter;
        [SerializeField] Transform shootOrigin;
        [SerializeField] GameObject bulletPrefab;
        [SerializeField] AnimationCurve fadeCurve;
        [SerializeField] float shootPower = 10f;
        [SerializeField] Slider sliderShootPower;
        [SerializeField] AudioClip[] audioClips;
        [SerializeField] AudioSource[] audioSources;

        void Start()
        {
            this.shooter.onShoot += this.Shoot;
            StartCoroutine(this.CoChangeSliderValue());
        }

        internal void Shoot(Ray ray)
        {
            var bullet = Instantiate(this.bulletPrefab, this.shootOrigin.position, Quaternion.LookRotation(ray.direction, Vector3.up) * Quaternion.AngleAxis(90, Vector3.right), null);
            var fadeOuter = bullet.AddComponent<FadeOuter>();
            fadeOuter.FadeOut(10f, 1f, this.fadeCurve);
            if (bullet.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.velocity = ray.direction * this.shootPower * (this.sliderShootPower.value + 1);
            }
            if (bullet.TryGetComponent(out Bullet bullet1))
            {
                bullet1.onFragment += this.PlaySE;
            }
        }

        IEnumerator CoChangeSliderValue()
        {
            while (true)
            {
                yield return null;
                var time = Time.time;
                int floor = Mathf.FloorToInt(time);
                if (0 == floor % 2)
                {
                    this.sliderShootPower.SetValueWithoutNotify(time - floor);
                }
                else
                {
                    this.sliderShootPower.SetValueWithoutNotify(1 - (time - floor));
                }
            }   
        }

        void PlaySE(Vector3 point)
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
                audioSource.transform.position = point;
                audioSource.Play();
            }
        }

        int nextAudioSourceIndex = 0;
    }
}