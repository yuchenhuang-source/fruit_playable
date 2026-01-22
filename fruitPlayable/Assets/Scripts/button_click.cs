using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class button_click : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform canvas;
    public GameObject explosion_particle;
    public Animator UI_ani;
    public Transform UI_transform;
    private Vector3 new_scale=new Vector3(1f,1f,1f);
    //public Vector3 new_scale2;
    public bool isAnimating = false;
    private Vector3 originalScale; // 记录原始大小
    private Sequence currentSequence;
    void Start()
    {
        Debug.Log("this is version2");
        // 2. 游戏开始时获取组件
        originalScale = UI_transform.localScale;
    }
    public void OnClick_dotween()
    {
        Dotween1();
        
        UI_ani.SetBool("play",false);
    }
    public void OnClick_animation()
    {
        Dotween2();
        //DotweenAnimation2();
        //Debug.Log("OnClick");
        UI_ani.SetBool("play",true);
    }
    private void Dotween1(){
        //PlayParticleEffect();
        EffectPlayManager.Instance.PlayEffect(explosion_particle,canvas);
        AudioMgr.Instance.Play("click");
        // 停止当前正在运行的动画
        if(currentSequence != null && currentSequence.IsActive()){
            currentSequence.Kill();
            Debug.Log("Dotween1 is stopped");
        }
        isAnimating = true;

        currentSequence = DOTween.Sequence();
        currentSequence.Append(UI_transform.DOScale(new_scale,0.8f).SetEase(Ease.OutBack));
       currentSequence.OnComplete(()=>{
         isAnimating=false;
         currentSequence=null;
       });

    }
    private void PlayParticleEffect()
    {
        if (explosion_particle == null || canvas == null) return;
        
        // 实例化粒子特效
        GameObject effect = Instantiate(explosion_particle, canvas);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
        
        // 播放所有粒子系统
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            ps.Play();
        }
        
        // 计算最大持续时间并自动销毁
        float maxDuration = 0f;
        foreach (ParticleSystem ps in particles)
        {
            if (!ps.main.loop)
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (duration > maxDuration)
                {
                    maxDuration = duration;
                }
            }
        }
        
        // 如果找到有效时长，延时销毁
        if (maxDuration > 0)
        {
            Destroy(effect, maxDuration);
        }
    }
    
    private void Dopunch(){
        UI_transform.DOPunchScale(new Vector3(1f,1f,1f), 0.2f, 5, 1);
    }


    private void Dotween2(){
        // 停止当前正在运行的动画
        if (currentSequence != null && currentSequence.IsActive())
        {
            Debug.Log("Dotween2 is stopped");
            currentSequence.Kill();
        }
        
        isAnimating = true;

        currentSequence = DOTween.Sequence();
        currentSequence.Append(UI_transform.DOScale(new Vector3(0f,0f,0f), 0.8f).SetEase(Ease.Linear));

       
        currentSequence.OnComplete(() => 
        {
            isAnimating = false;
            currentSequence = null;
        });

    }
    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnDestroy()
    {
        // 清理正在运行的动画，防止内存泄漏
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
    }
}
