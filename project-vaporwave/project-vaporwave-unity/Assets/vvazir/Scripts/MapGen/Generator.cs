﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    [Range(0f, 1f)]
    public float threshold = .8f;
    [Range(100, 1000)]
    public int resolution = 100;
    [Range(0f, 1f)]
    public float chunkSize = .2f;
    public float largeAsteroidSize = 2f;
    public Vector3 offset;
    public Vector3 rotation;

    public float frequency = 1f;

    [Range(0f, 1f)]
    public float strength = 1f;

    public bool damping;
    [Range(1, 8)]
    public int octaves = 1;

    [Range(1f, 4f)]
    public float lacunarity = 2f;

    [Range(0f, 1f)]
    public float persistence = 0.5f;

    [Range(1, 3)]
    public int dimensions = 3;

    public NoiseMethodType type;

    public int asteroidLimit = 10;

    private int currentResolution;

    private Object[] asteroids;
    private int count = 0;
    private GameObject go;
    private Texture2D texture;
 
    // Use this for initialization
    void Start()
    {
        Application.targetFrameRate = 120;
        go = this.gameObject;
        asteroids = Resources.LoadAll("Asteroids/Prefabs/Asteroids", typeof(GameObject));
        CreateAsteroids();
        MergeAsteroids();
        Debug.Log(go.transform.childCount);
    }
    private void OnEnable()
    {
        /*
        if (texture == null)
        {
            texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, true)
            {
                name = "Procedural Texture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 9
            };
            GetComponent<MeshRenderer>().material.mainTexture = texture;
        }
        */
    }
    public void MergeAsteroids()
    {
        foreach (Transform checkingChild in go.transform)
        {
            if (checkingChild.gameObject != null && checkingChild.parent!=null)
            {
                MeshCollider mc = checkingChild.GetComponent<MeshCollider>();
                foreach (Transform checkChild in go.transform)
                {
                    if (checkChild.gameObject != null && checkChild.parent!=null && checkingChild.name != checkChild.name)
                    {
                        MeshCollider tmpMC = checkChild.GetComponent<MeshCollider>();
                        if (mc.bounds.Intersects(tmpMC.bounds))
                        {
                            
                            //Debug.Log(System.String.Format("Merging {0} with {1}",checkingChild.name,checkChild.name));
                            //Debug.Log(checkingChild.transform.localScale.magnitude);
                            checkingChild.transform.localScale += Vector3.one * (tmpMC.transform.localScale.x * 2);
                            //Debug.Log(checkingChild.transform.localScale.magnitude);
                            checkChild.parent = null;
                            GameObject.Destroy(checkChild.gameObject);
                            //Debug.Log(checkChild.gameObject == null);
                            count--;
                        }
                    }
                }
            }
        }
        
    }
    public void CreateAsteroids()
    {
        foreach (Transform child in go.transform)
        {
            child.parent = null;
            GameObject.Destroy(child.gameObject);
            count--;
        }
        count = 0;
        float stepSize = 1f / resolution;
        NoiseMethod method = Noise.noiseMethods[(int)type][dimensions - 1];
        int scale = Mathf.FloorToInt(chunkSize * resolution);
        long scale3 = (long)Mathf.Pow(scale, 3f);        
        if (chunkSize != 0f)
        {
            Vector3 point00 = transform.TransformPoint(new Vector3(-0.5f, -0.5f));
            Vector3 point10 = transform.TransformPoint(new Vector3(0.5f, -0.5f));
            Vector3 point01 = transform.TransformPoint(new Vector3(-0.5f, 0.5f));
            Vector3 point11 = transform.TransformPoint(new Vector3(0.5f, 0.5f));
            Vector3 zpoint0 = transform.TransformPoint(new Vector3(0, 0, -0.5f));
            Vector3 zpoint1 = transform.TransformPoint(new Vector3(0, 0, 0.5f));
            float sum = 0f;
            //long pos = (long)Mathf.Pow(resolution, 3f);
            //for (long currPos = 0; currPos < pos; currPos += scale3)
            //{

            //    long z = currPos % resolution;
            //    long x = currPos % (long)Mathf.Pow(z, 2f);
            //    long y = currPos % (long)Mathf.Pow(x, 3f);
            //}
            for (int y = 0; y < resolution; y += 1)
            {
                int yi = y % scale;
                Vector3 point0 = Vector3.Lerp(point00, point01, (y + 0.5f) * stepSize);
                Vector3 point1 = Vector3.Lerp(point10, point11, (y + 0.5f) * stepSize);
                for (int x = 0; x < resolution; x += 1)
                {
                    int xi = x % scale;
                    Vector3 point = Vector3.Lerp(point0, point1, (x + 0.5f) * stepSize);
                    for (int z = 0; z < resolution; z += 1)
                    {
                        int zi = z % scale;
                        Vector3 zpoint = Vector3.Lerp(zpoint0, zpoint1, (z + 0.5f) * stepSize);
                        point += zpoint;

                        point.z = (z + 0.5f) * stepSize;
                        float sample = Noise.Sum(method, point * 1f, frequency, octaves, lacunarity, persistence).value;
                        if (type != NoiseMethodType.Value)
                        {
                            sample = sample * 0.5f + 0.5f;
                        }
                        sum += sample;

                        if ((sum > (threshold * (scale * scale * scale)) * largeAsteroidSize) || (yi == scale / 2 && xi == scale / 2 && zi == scale / 2))
                        {
                            if (sum > threshold * (scale * scale * scale) && count < asteroidLimit)
                            {
                                //Debug.Log(sum);
                                count++;
                                GameObject tmp = (GameObject)Instantiate(
                                    asteroids[(Random.Range(0, asteroids.Length))],                            // Random Asteroid
                                    new Vector3((x - .5f) * 1f,                          // Random Location
                                                (y - .5f) * 1f,
                                                (z - .5f) * 1f),
                                    Quaternion.identity);                                           // Same rotation

                                tmp.tag = "Asteroid";
                                tmp.AddComponent<vars>();
                                tmp.name = tmp.name.Replace("(Clone)", (System.String.Format("({0})", count))).Trim();
                                vars v = tmp.GetComponent<vars>();
                                
                                v.go = tmp;
                                v.maxHP =(int)((sum) * (1f / ((scale * scale * scale))) * 1000);
                                v.currHP = tmp.GetComponent<vars>().maxHP;
                                v.mc = tmp.AddComponent<MeshCollider>();
                                v.mc.convex = true;
                                tmp.transform.parent = go.transform;                                // Set as child of main
                                tmp.transform.localScale *= (sum) * (1f / ((scale * scale * scale)));                        // Scale based on position
                                tmp.AddComponent<Rigidbody>();
                                v.rb = tmp.GetComponent<Rigidbody>();
                                v.rb.isKinematic = false;
                                v.rb.angularVelocity = Random.insideUnitSphere * Random.Range(.01f, 1f);
                                v.rb.drag = 0;
                                v.rb.angularDrag = 0;
                                v.rb.useGravity = false;
                                v.rb.mass = v.maxHP;
                                


                            }
                            sum = 0f;
                        }

                        if (count >= asteroidLimit)
                        {
                            break;
                        }
                    }
                    if (count >= asteroidLimit)
                    {
                        break;
                    }
                }
                if (count >= asteroidLimit)
                {
                    break;
                }
            }
        }
        
        //texture.Apply();

    }

    // Update is called once per frame
    //void Update () {
            
    //}
}