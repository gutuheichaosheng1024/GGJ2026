using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;



namespace Manager
{
    [System.Serializable]
    public class SettingData
    {
        public float BackgroundVolume = 1.0f;
        public float EffectVolume = 1.0f;
    }


    public class SettingManager : MonoBehaviour
    {
        public static SettingData _data;
        [SerializeField]private Slider Bslider;
        [SerializeField]private Slider Eslider;

        private void Awake()
        {
            Bslider = transform.Find("Background").GetComponent<Slider>();
            Eslider = transform.Find("Effect").GetComponent<Slider>();

            Bslider.value = _data.BackgroundVolume;
            Bslider.onValueChanged.AddListener(value => {
                _data.BackgroundVolume = value;
                SoundManager.audioSource.volume = value;
            });

            Eslider.value = _data.EffectVolume;
            Eslider.onValueChanged.AddListener(value => _data.EffectVolume = value);
        }




        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]//游戏启动时初始化
        private static void Initalize()
        {
            _data = SaveSystem.load<SettingData>();
            if(_data == null)_data = new SettingData();
        }

        private void OnApplicationQuit()
        {
            Debug.Log("?");
            SaveSystem.save<SettingData>(_data);
        }
    }
}

