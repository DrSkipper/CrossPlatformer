using UnityEngine;
using System.Collections;

public class Timer
{
	public delegate void TimerCallback();
	public TimerCallback callback;
	public bool paused;
	public bool loops;
	public bool completed { get; private set; }

	public Timer(float durationSeconds, bool loops = false, bool startsImmediately = true, TimerCallback callback = null)
	{
		_timeRemaining = _durationSeconds = durationSeconds;
		this.loops = loops;
		this.callback = callback;
		this.paused = !startsImmediately;
	}

	public void start()
	{
		this.paused = false;
	}

    public void reset()
    {
        _timeRemaining = _durationSeconds;
        this.completed = false;
    }

    public void complete()
    {
        if (this.callback != null)
            this.callback();

        if (this.loops)
            _timeRemaining = _durationSeconds;
        else
            this.completed = true;
    }

	public void update()
	{
		if (!this.paused && !this.completed)
		{
			_timeRemaining -= Time.deltaTime;

			if (_timeRemaining <= 0.0f)
                this.complete();
		}
	}

	public void invalidate()
	{
		this.callback = null;
		this.completed = true;
	}

	/**
	 * Private
	 */
	private float _durationSeconds;
	private float _timeRemaining;
}
