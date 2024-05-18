using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class FlowerArea : MonoBehaviour
{
    public const float _AreaDiameter = 20f;

    // list of all plants (they have many flowers)
    private List<GameObject> _FlowerPlants;

    // looking up for flowers by nectar collider
    private Dictionary<Collider, Flower> _FlowerWithNectar;

    // list of all flowers in area
    public List<Flower> _Flowers { get; private set; }

    /// <summary>
    /// Resets all flowers in area
    /// </summary>
    public void ResetFlowers()
    {
        foreach (var flowerPlant in  _FlowerPlants)
        {
            float xrot = UnityEngine.Random.Range(-5f, 5f);
            float yrot = UnityEngine.Random.Range(-180f,180f);
            float zrot = UnityEngine.Random.Range(-5f, 5f);

            flowerPlant.transform.rotation = Quaternion.Euler(xrot, yrot, zrot);
        }

        foreach (var flower in _Flowers)
        {
            flower.ResetFlower();
        }
    }

    /// <summary>
    /// Gets the <see cref="Flower"/> that a nectar belong to
    /// </summary>
    /// <param name="collider">Nectar collider</param>
    /// <returns>The matching flower</returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return _FlowerWithNectar[collider];
    }

    private void Awake()
    {
        _FlowerPlants = new List<GameObject>();
        _FlowerWithNectar = new Dictionary<Collider, Flower>();
        _Flowers = new List<Flower>();
    }

    private void Start()
    {
        FindChildFlowers(transform);
    }

    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);

            if (child.CompareTag("flower_plant"))
            {
                _FlowerPlants.Add(child.gameObject);

                FindChildFlowers(child);
            }
            else
            {
                if (child.TryGetComponent<Flower>(out var flowerComponent))
                {
                    _Flowers.Add(flowerComponent);

                    _FlowerWithNectar.Add(flowerComponent._NectarCollider, flowerComponent);
                }
                else
                {
                    FindChildFlowers(child);
                }
            }
        }
    }
}
