
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameConfig : MonoSingleton<GameConfig>
{
    [LunaPlaygroundField("isEnableBGM（是否启用BGM）", 0, "游戏配置参数")]
    public bool isEnableBGM = true;
    [LunaPlaygroundField("targetStepCount（单次目标步数）", 0, "游戏跳转参数")]
    public int targetStepCount = 999;
    [LunaPlaygroundField("cycleStepCount（操作循环次数）", 0, "游戏跳转参数")]
    public int cycleStepCount = 3;
    [LunaPlaygroundField("targetCompleteCount（目标完成次数）", 0, "游戏跳转参数")]
    public int targetCompleteCount = 999;
    [LunaPlaygroundField("targetLevel（目标关卡）", 0, "游戏跳转参数")]
    public int targetLevel = 999;
    int curStepCount = 0;
    int curCompleteCount = 0;
    [LunaPlaygroundField("MaxIdleTime（最大空闲时间）", 0, "游戏跳转参数")]
    public float MaxIdleTime = 8f;
    float curIdleTime = 0f;
    bool isStartIdle = false;
    bool isPlayBGM = false;

    void Update()
    {
        if (isStartIdle)
        {
            curIdleTime += Time.deltaTime;
        }
    }

    public void AddTargetStepCount()
    {
        curStepCount++;
        if (curStepCount >= targetStepCount || cycleStepCount <= 0)
        {
            cycleStepCount--;
            curStepCount = 0;
            Utils.OpenStore();
        }
    }

    public void AddTargetCompleteCount()
    {
        curCompleteCount++;
        if (curCompleteCount >= targetCompleteCount)
        {
            Utils.OpenStore();
        }
    }

    public void OpRecord()
    {
        isStartIdle = true;
        if (curIdleTime >= MaxIdleTime)
        {
            Utils.OpenStore();
        }
        curIdleTime = 0f;
    }

    public void PlayBGM()
    {
        if (!isPlayBGM)
        {
            if (isEnableBGM && AudioMgr.Instance.BGM != null)
            {
                AudioMgr.Instance.Play(AudioMgr.Instance.BGM.name, AudioMgr.AudioType.BGM);
            }
            isPlayBGM = true;
        }
    }
}
