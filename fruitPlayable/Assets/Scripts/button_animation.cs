using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class button_animation : MonoBehaviour
{
    private Sequence currentSequence;
    private Vector3 originalScale;
    // Start is called before the first frame update
    void Start()
    {
        originalScale=transform.localScale;
        PlaySequence();
    }
    private void PlaySequence(){
        currentSequence=DOTween.Sequence();
        currentSequence.Append(transform.DOPunchScale(originalScale*0.3f,0.5f,5,1f))
        .AppendInterval(0.2f)
        .Append(transform.DOPunchScale(originalScale*0.3f,0.5f,5,1f))
        .AppendInterval(1f).SetLoops(-1,LoopType.Restart);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
