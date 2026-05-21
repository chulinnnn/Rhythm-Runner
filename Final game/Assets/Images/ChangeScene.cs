using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChangeScene : MonoBehaviour
{
    //按钮
    private Button clickBtn;
    //场景索引
    public int sceneIndex;
    
    // Start is called before the first frame update
    void Start()
    {
        //获取按钮
        clickBtn = GetComponent<Button>();
        //如果按钮不为空
        if (clickBtn != null)
        {
            //添加点击事件
            clickBtn.onClick.AddListener(LoadScene);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //读取场景
    private void LoadScene()
    {
        //读取场景
        SceneManager.LoadScene(sceneIndex);
    }
    //选择
    public void OnChangeScene(int sceneIndex)
    {
        //选择场景
        SceneManager.LoadScene(sceneIndex);
    }
    //退出
    public void EG()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
