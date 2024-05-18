using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages nectar count
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("Color when flower full of nectar")]
    public Color _FullColor = new(1f, 0f, .3f);

    [Tooltip("Color when flower empty of nectar")]
    public Color _EmptyColor = new(.5f, 0f, 1f);

    /// <summary>
    /// trigger that allows Hummingbird drink nectar
    /// </summary>
    [HideInInspector]
    public Collider _NectarCollider;

    // Solid collider representing flowers petals
    Collider _FlowerCollider;

    // flowers material
    Material _FlowersMaterial;

    /// <summary>
    /// Pointing straight out of the flower
    /// </summary>
    public Vector3 _FlowerUpVector
    {
        get
        {
            return _NectarCollider.transform.up;
        }
    }

    public Vector3 _FlowerCenterPosition
    {
        get
        {
            return _NectarCollider.transform.position;
        }
    }

    public float _NectarAmount { get; private set; }

    public bool _HasNectar
    {
        get { return _NectarAmount > 0f; }
    }

    /// <summary>
    /// Attempts to remove nectar from flower
    /// </summary>
    /// <param name="amount">The amount of nectar to remove (0f, 1f)</param>
    /// <returns>Actual nectar succesfully removed</returns>
    public float Feed(float amount) 
    { 
        _NectarAmount -= amount;

        if (_NectarAmount <= 0f) 
        {
            _NectarAmount = 0;

            // allows agent fly through petals
            _FlowerCollider.gameObject.SetActive(false);
            // disables agent to feed nectar
            _NectarCollider.gameObject.SetActive(false);
        
            _FlowersMaterial.SetColor("_BaseColor", _EmptyColor);
        }

        return Mathf.Clamp(amount, 0f, _NectarAmount);
    }

    public void ResetFlower()
    {
        _NectarAmount = 1f;

        _FlowerCollider.gameObject.SetActive(true);
        _NectarCollider.gameObject.SetActive(true);

        _FlowersMaterial.SetColor("_BaseColor", _FullColor);
    }

    private void Awake()
    {
        _FlowersMaterial = GetComponent<MeshRenderer>().material;

        _FlowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        _NectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();

        ResetFlower();
    }
}
