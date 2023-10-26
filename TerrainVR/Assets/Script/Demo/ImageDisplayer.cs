using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageDisplayer : MonoBehaviour
{
    public Texture[] textures;

    private Renderer renderer;
    private int index = 0;
    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<Renderer>();
        renderer.material.SetTexture("_MainTex", textures[index]);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            index++;
            if (index >= textures.Length) index = 0;
            renderer.material.SetTexture("_MainTex", textures[index]);
        }
    }
}
