using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class tile_behaviour : MonoBehaviour
{
    private Sequence currentSequence;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void OnClick_tile(){

    }
    private void PlaySequence(){
        currentSequence=DOTween.Sequence();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
