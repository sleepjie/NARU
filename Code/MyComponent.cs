using Sandbox;
using SpriteTools;
using System;

public sealed class MyComponent : Component
{
	[RequireComponent] SpriteComponent Sprite { get; set; }
	[Property] float Speed { get; set; } = 200f;
	[Property] float MaxJumpHeight { get; set; } = 150f;
	[Property] float JumpForce { get; set; } = 300f;
	[Property] float Gravity { get; set; } = 800f;
	[Property] float FallMultiplier { get; set; } = 2.5f;
	[Property] float LowJumpMultiplier { get; set; } = 2f;
	[Property] float DashSpeed { get; set; } = 400f;
	[Property] float DashDuration { get; set; } = 0.2f;
	[Property] float GroundCheckDistance { get; set; } = 5f;

	SceneTrace trace { get; set; }

	Vector2 Velocity = Vector2.Zero;
	Vector2 DashDirection = Vector2.Zero;
	bool IsGrounded = true;
	bool WasGrounded = true;
	bool IsFalling = false;
	bool IsDashing = false;
	float DashTimer = 0f;
	float InitialJumpY;
	bool IsJumping = false;
	protected override void OnFixedUpdate()
	{
		ApplyGravity();
		CheckGroundStatus();
		BuildWishVelocity();
		Move();
	}

	protected override void OnStart()
	{
		Sprite.OnBroadcastEvent += OnBroadcastEvent;
		Sprite.OnAnimationComplete += OnAnimationComplete;
	}

	void BuildWishVelocity()
	{
		var input = Input.AnalogMove;

		// Horizontal movement
		Velocity.x = -input.y * Speed; // A/D keys for left/right movement

		// Initiate jump with W key
		if ( input.x > 0 && IsGrounded )
		{
			Log.Info( "Jumping!" );
			IsJumping = true;
			IsGrounded = false;
			InitialJumpY = WorldPosition.y;
			Velocity.y += JumpForce;
			Sprite.PlayAnimation( "Jump" );
		}
		if ( input.y > 0 && IsGrounded && !IsFalling )
		{
			// Sustain jump while W is held and max height not reached
			if ( IsJumping && input.y > 0 && WorldPosition.y > InitialJumpY - MaxJumpHeight )
			{
				Velocity.y = -JumpForce * Time.Delta;
			}
		}
		else
		{
			IsJumping = false;
		}

		// Increase fall speed with S key
		if ( -input.y > 0 && !IsGrounded )
		{
			Velocity.y -= Gravity * (FallMultiplier - 1) * Time.Delta;
		}
		// Initiate dash with Run key
		if ( Input.Pressed( "Run" ) && !IsDashing && Velocity.Length > 0 )
		{
			StartDash( input.Normal );
		}
	}

	void ApplyGravity()
	{
		if ( !IsGrounded )
			Velocity.y -= Gravity * Time.Delta;
	}

	void StartDash( Vector2 direction )
	{
		IsDashing = true;
		DashDirection = direction;
		DashTimer = DashDuration;
		Velocity = DashDirection * DashSpeed; // Set initial dash velocity
		Sprite.PlayAnimation( "Dash" );
	}

	void PerformDash()
	{
		DashTimer -= Time.Delta;
		if ( DashTimer >= 0 )
		{
			Velocity = DashDirection * DashSpeed; // Continuously apply dash velocity
		}
		else
		{
			EndDash();
		}
	}

	void EndDash()
	{
		IsDashing = false;
		Velocity = Vector2.Lerp( Velocity, Vector2.Zero, 10f * Time.Delta );
		Sprite.PlayAnimation( "Idle" );
	}
	void CheckGroundStatus()
	{
		var groundCheckDistance = Sprite.Bounds.Maxs.y + 10f;

		var start = WorldPosition + new Vector3( Vector2.Up );
		var end = start + new Vector3( Vector2.Down ) * groundCheckDistance;

		var trace = Scene.Trace.Ray( start, end ).WithTag( "Ground" ).Run();
		IsGrounded = trace.Hit;
		if ( IsGrounded )
		{
			WorldPosition = WorldPosition.WithY( trace.HitPosition.y + Sprite.Bounds.Size.y / 2f + 4f );
			Velocity.y = 0;
			Log.Info( "Landing!" );
		}
	}

	void Move()
	{
		var position = WorldPosition + new Vector3( Velocity ) * Time.Delta;
		WorldPosition = position;
	}

	protected override void OnUpdate()
	{
		UpdateAnimations();
	}

	void OnBroadcastEvent( string name )
	{
		if ( name == "footstep" && IsGrounded )
		{
			Sound.Play( "ui.button.press", WorldPosition );
		}
	}

	void UpdateAnimations()
	{
		if ( IsDashing  )
		{
			Sprite.PlayAnimation( "Dash" );
		}
		else if ( !WasGrounded && IsGrounded )
		{
			Sprite.PlayAnimation( "Land" );
		}
		else if ( Velocity.x != 0 && IsGrounded )
		{
			Sprite.PlayAnimation( "Run" );
			Sprite.PlaybackSpeed = Velocity.x / Speed;
			Sprite.SpriteFlags = Velocity.x < 0 ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
		}
		else if ( IsGrounded && Velocity.y > 0 )
		{
			Sprite.PlayAnimation( "Jump" );
		}
		else if ( !IsGrounded && Velocity.y < 0 )
		{
			Sprite.PlayAnimation( "Fall" );
		}
		else if ( IsGrounded )
		{
			Sprite.PlayAnimation( "Idle" );
		}

		WasGrounded = IsGrounded;
	}

	void OnAnimationComplete( string name )
	{
		if ( name == "DashEnd" )
		{
			Sprite.PlayAnimation( "Idle" );
			IsDashing = false; // Reset dash state
		}
	}
}
