using UnityEngine;
using System.Collections;

/**
 * https://twitter.com/AsherVo/status/461579941159501824
 */
public class VoBehavior : MonoBehaviour
{
	private GameObject _gameObject;
	public new GameObject gameObject
	{
		get
		{
			if (!_gameObject)
				_gameObject = base.gameObject;
			return _gameObject;
		}
	}

	private Transform _transform;
	public new Transform transform
	{
		get
		{
			if (!_transform)
				_transform = base.transform;
			return _transform;
		}
	}

	private Renderer _renderer;
	public new Renderer renderer
	{
		get
		{
			if (!_renderer)
				_renderer = base.GetComponent<Renderer>();
			return _renderer;
		}
	}
	
	private SpriteRenderer _spriteRenderer;
	public SpriteRenderer spriteRenderer
	{
		get
		{
			if (!_spriteRenderer)
				_spriteRenderer = base.GetComponent<Renderer>() as SpriteRenderer;
			return _spriteRenderer;
		}
	}
}
