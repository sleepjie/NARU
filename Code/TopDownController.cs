using Sandbox;
using SpriteTools;

public sealed class TopDownController : Component
{
	[RequireComponent] SpriteComponent Sprite { get; set; }
	[Property] float Speed { get; set; } = 300f;
	[Property] float DashSpeed { get; set; } = 600f;
	[Property] float DashDuration { get; set; } = 0.2f;
	[Property] public GameObject Camera { get; set; }

	[Property] float ComboCooldown { get; set; } = 0.5f; // Time to press the next attack
	[Property] float AttackRange { get; set; } = 50f;
	[Property] int AttackDamage { get; set; } = 10;

	private string[] ComboSequence = { "Attack1", "Attack2", "Attack3" }; // Animations for combo attacks
	private int CurrentComboIndex = 0;
	private float ComboTimer = 0f;
	private bool IsAttacking = false;

	Vector2 Velocity = Vector2.Zero;
	Vector2 DashDirection = Vector2.Zero;
	Vector2 LastDirection = new Vector2( 0, -1 ); // Default facing direction is up
	float DashTimer = 0f;
	bool IsDashing = false;

	protected override void OnStart()
	{
		// Subscribe to the OnBroadcastEvent delegate
		Sprite.OnBroadcastEvent += OnBroadcastEvent;
	}


	void OnBroadcastEvent( string name )
	{
		if ( name == "DashStart" )
		{
			StartDash();
		}
		else if ( name == "DashEnd" )
		{
			EndDash();
		}
		else if ( name == "footstep" )
		{
			Sound.Play( "ui.button.press", WorldPosition );
			Log.Info( "Footstep sound!" );
		}
		else if ( name == "AttackHitFrame" )
		{
			PerformAttack();
		}
		else if ( name == "AttackEnd" )
		{
			if ( ComboTimer > 0 && CurrentComboIndex < ComboSequence.Length - 1 )
			{
				AdvanceCombo();
			}
			else
			{
				ResetCombo();
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsDashing )
		{
			PerformDash();
		}
		else
		{
			HandleInput();
		}

		Move();
	}

	protected override void OnUpdate()
	{
		UpdateAnimations();

		// Reduce combo timer
		if ( ComboTimer > 0 )
		{
			ComboTimer -= Time.Delta;
		}
	}

	void HandleInput()
	{
		var input = Input.AnalogMove;

		// Map W, S, A, D to appropriate directions
		Velocity.x = -input.y * Speed;  // A/D keys control horizontal movement
		Velocity.y = input.x * Speed;   // W/S keys control vertical movement

		// Update last direction when moving
		if ( Velocity.Length > 0 )
		{
			LastDirection = Velocity.Normal;
		}

		// Initiate dash if "Run" is pressed
		if ( Input.Pressed( "Run" ) )
		{
			// Broadcast the DashStart event to trigger the dash animation
			StartDash();
		}

		if ( Input.Pressed("Attack" ) ) // Trigger attack
		{
			if ( ComboTimer > 0 ) // Continue combo
			{
				AdvanceCombo();
			}
			else // Start new combo
			{
				StartCombo();
			}
		}
	}

	void StartDash()
	{
		IsDashing = true;
		DashDirection = LastDirection;
		DashTimer = DashDuration;
	}

	void PerformDash()
	{
		Velocity = DashDirection * DashSpeed;
		DashTimer -= Time.Delta;

		if ( DashTimer <= 0 )
		{
			EndDash();
		}
	}

	void EndDash()
	{
		IsDashing = false;
		Velocity = Vector2.Zero;
	}


	void StartCombo()
	{
		IsAttacking = true;
		CurrentComboIndex = 0;
		ComboTimer = ComboCooldown;
		PlayComboAnimation();
	}

	void AdvanceCombo()
	{
		IsAttacking = true;
		CurrentComboIndex++;
		ComboTimer = ComboCooldown;
		PlayComboAnimation();
	}

	void ResetCombo()
	{
		IsAttacking = false;
		CurrentComboIndex = 0;
		ComboTimer = 0f;
	}

	void PlayComboAnimation()
	{
		if ( CurrentComboIndex >= ComboSequence.Length ) return;

		var animation = ComboSequence[CurrentComboIndex];

		// Play the appropriate attack animation based on LastDirection
		if ( LastDirection.y >  LastDirection.x )
		{
			Sprite.PlayAnimation( $"{animation}Down" );
		}
		else if ( LastDirection.y < -LastDirection.x )
		{
			Sprite.PlayAnimation( $"{animation}Up" );
		}
		else
		{
			Sprite.PlayAnimation( $"{animation}Side" );
			Sprite.SpriteFlags = LastDirection.x < 0 ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
		}
	}

	void PerformAttack()
	{
		// Create a transient hitbox in front of the player
	//	var hitboxCenter = WorldPosition + new Vector3( LastDirection.x, LastDirection.y, 0 ) * AttackRange;

		// Detect enemies within the hitbox
		//var entities = Scene.FindInPhysics( hitboxCenter);
		//foreach ( var entity in entities )
	//	{
		//	if ( entity is IEnemy enemy ) // Assuming an IEnemy interface
			//{
				//enemy.TakeDamage( AttackDamage );
		//	}
		//}
	}

	void Move()
	{
		WorldPosition += new Vector3( Velocity.x, Velocity.y, 0 ) * Time.Delta;
	}

	void UpdateAnimations()
	{
		if ( IsDashing )
		{
			PlayDashAnimation();
		}
		else if ( Velocity.Length > 0 )
		{
			PlayMovementAnimation();
		}
		else
		{
			PlayIdleAnimation();
		}
	}

	void PlayDashAnimation()
	{
		if ( -Velocity.y > Velocity.y )
		{
			Sprite.PlayAnimation( "DashDown" );
		}
		else if ( -Velocity.y < Velocity.y )
		{
			Sprite.PlayAnimation( "DashUp" );
		}
		else
		{
			Sprite.PlayAnimation( "DashSide" );
			Sprite.SpriteFlags = Velocity.x < 0 ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
		}
	}

	void PlayMovementAnimation()
	{
		if ( -Velocity.y > Velocity.y )
		{
			Sprite.PlayAnimation( "WalkDown" );
		}
		else if ( -Velocity.y < Velocity.y )
		{
			Sprite.PlayAnimation( "WalkUp" );
		}
		else
		{
			Sprite.PlayAnimation( "WalkSide" );
			Sprite.SpriteFlags = Velocity.x < 0 ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
		}
	}

	void PlayIdleAnimation()
	{
		if ( LastDirection.y > -LastDirection.y )
		{
			Log.Info( "Idling up" );
			Sprite.PlayAnimation( "IdleUp" );
		}
		else if ( LastDirection.y < -LastDirection.y )
		{
			Log.Info( "Idling down" );
			Sprite.PlayAnimation( "IdleDown" );
		}
		else
		{
			Log.Info( "Idling side" );
			Sprite.PlayAnimation( "IdleSide" );
			Sprite.SpriteFlags = LastDirection.x < 0 ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
		}
	}
}
