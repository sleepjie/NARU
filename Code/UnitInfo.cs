using Sandbox;
using System;
public enum UnitType
{
	None,
	Player,
	Enemy
}

public sealed class UnitInfo : Component
{
	[Property]
	[Range( 0f, 2f, 0.1f )]
	public float DelayDeath { get; set; } = 0f;
	public float Health { get; private set; }
	public bool Alive { get; private set; } = true;
	public event Action<float> OnDamage;
	public event Action OnDeath;
	TimeSince _lastDamage;
	TimeUntil _nextHeal;
	[Property] public UnitType Team { get; set; }

	[Property][Range( 0.1f, 10f, 0.1f )]
	public float MaxHealth { get; set; } = 5f;

	protected override void OnStart()
	{
		Health = MaxHealth;
	}

	public void Damage( float damage )
	{
		if ( !Alive ) return;

		Health = Math.Clamp(Health - damage, 0f, MaxHealth);

		if ( damage > 0 )
			_lastDamage = 0f;

		OnDamage?.Invoke( damage );

		if ( Health <= 0 )
			Kill();
	}

	/// <summary>
	/// Set the HP to 0 and Alive to false, then destroys it
	/// </summary>
	public async void Kill()
	{
		Health = 0f;
		Alive = false;

		OnDeath?.Invoke();

		await Task.DelaySeconds( DelayDeath );

		GameObject.Destroy();
	}
}
