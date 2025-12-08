using System;
using UnityEngine;

namespace PreTestFunc.Noise
{
    public class NoiseVisualizeQuad : MonoBehaviour
    {
        public GameObject prefabQuad;

        public Vector2 gridSize = new(20, 20);

        public float spacing = 1.5f;
        
        private void Start()
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    Vector3 position = new Vector3(x * spacing, 0, z * spacing);
                    GameObject quad = Instantiate(prefabQuad, position, Quaternion.identity, transform);

                    quad.transform.eulerAngles = new Vector3(90, 0, 0);

                    float noiseValue = SimpleNoise.ValueNoise2D(x * 0.1f, z * 0.1f);
                    // float noiseValue = Mathf.PerlinNoise(x * 0.1f, z * 0.1f);

                    Color color = new Color(noiseValue, noiseValue, noiseValue);

                    Renderer rd = quad.GetComponent<Renderer>();
                    if (rd != null)
                        rd.material.color = color;
                }
            }
        }
    }
}