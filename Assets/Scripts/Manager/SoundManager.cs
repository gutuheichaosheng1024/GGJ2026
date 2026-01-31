using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Manager
{



    public class SoundManager : Singleton<SoundManager>
    {
        private static string BasePath = "Music";
        private static int MaxEffectSouce = 10;

        private static Dictionary<string, AudioClip> audioResources = new Dictionary<string, AudioClip>();
        private static Dictionary<GameObject, List<AudioSource>> audioPlayers = new Dictionary<GameObject,List<AudioSource>>();
        public static AudioSource audioSource;

        /// <summary>
        /// <paramref name="_musicName" /> =音乐名称/路径
        /// </summary>
        public static void PlayBackGroundSound(string _musicName,bool loop = true)//BGM同时只能存在一份
        {
            if(audioSource == null)
            {
                audioSource = Instance.gameObject.AddComponent<AudioSource>();
            }
            AudioClip clip;
            if (!audioResources.TryGetValue(_musicName,out clip))
            {
                string filePath = Path.Combine(BasePath, "Background", _musicName);
                clip = Resources.Load<AudioClip>(filePath);
                audioResources.Add(_musicName, clip);
            }


            audioSource.clip = clip;
            audioSource.volume = SettingManager._data.BackgroundVolume;
            audioSource.loop = loop;
            audioSource.Play();
        }

        /// <summary>
        /// <paramref name="_musicName" /> =音乐名称/路径
        /// <paramref name="_self" /> =播放音效的物体本身
        /// <paramref name="times" /> =播放次数(待实现)
        /// </summary>
        public static void PlayEffect(string _musicName,GameObject _self, int times = 1)
        {
            if (times != 1) throw new System.Exception("未实现");
            AudioClip clip;
            if (!audioResources.TryGetValue(_musicName, out clip))
            {
                string filePath = Path.Combine(BasePath, "Effect", _musicName);
                clip = Resources.Load<AudioClip>(filePath);
                audioResources.Add(_musicName, clip);
            }

            List<AudioSource> audioSources;
            if (!audioPlayers.TryGetValue(_self, out audioSources))
            {
                audioPlayers.Add(_self, new List<AudioSource>());
                audioSources = audioPlayers[_self];
            }

            foreach (AudioSource source in audioSources)
            {
                if (!source.isPlaying)
                {
                    source.clip = clip;
                    source.volume = SettingManager._data.EffectVolume;
                    source.loop = false;
                    source.Play();
                    return;
                }
            }

            if(audioSources.Count < MaxEffectSouce)
            {
                AudioSource source = _self.AddComponent<AudioSource>();
                audioSources.Add(source);
                source.clip = clip;
                source.volume = SettingManager._data.EffectVolume;
                source.loop = false;
                source.Play();
            }

        }

        protected override void Awake()
        {
            base.Awake();
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        /// <summary>
        /// 场景卸载完成
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            audioPlayers.Clear();
            audioSource.Stop();
        }


    }


}

