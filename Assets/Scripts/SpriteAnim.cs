using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAnim : MonoBehaviour
{
    [SerializeField] Sprite[] sprites;
    [SerializeField] float Step;
    SpriteRenderer SR;
    int frame;
    float lastTime;
    // Start is called before the first frame update
    void Start()
    {
        SR = gameObject.GetComponent<SpriteRenderer>();
        frame = 0;
        lastTime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (lastTime+Step < Time.time)
        {
            Texture2D newTexture = Resources.Load<Texture2D>("NewTexture");
            SR.material.SetTexture("_BaseColorMap",sprites[frame].texture);
            frame++;
            lastTime = (float)Time.time;
            if (frame+1>sprites.Length)
            {
                frame = 0;
            }
        }
    }
}
