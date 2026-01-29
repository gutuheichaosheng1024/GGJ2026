using UnityEngine.UI;
using UnityEngine;



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
        private Slider Bslider;
        private Slider Eslider;

        private void Awake()
        {
            Bslider = GameObject.Find("Background").GetComponent<Slider>();
            Eslider = GameObject.Find("Effect").GetComponent<Slider>();


            Bslider.onValueChanged.AddListener(value => _data.BackgroundVolume = value);
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
            SaveSystem.save<SettingData>(_data);
        }
    }
}

