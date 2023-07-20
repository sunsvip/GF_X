/* UltimateJoystick.cs */
/* Written by Kaz Crowe */
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[ExecuteInEditMode]
public class UltimateJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
	// INTERNAL CALCULATIONS //
	RectTransform baseTrans;
	Vector2 defaultPos = Vector2.zero;
	Vector2 joystickCenter = Vector2.zero;
	int _inputId = -10;
	Rect joystickRect;
	CanvasGroup joystickGroup;
	float radius = 1.0f;
	public Canvas ParentCanvas
	{
		get;
		private set;
	}
	RectTransform canvasRectTrans;

	// JOYSTICK POSITIONING //
	public RectTransform joystickBase, joystick;
	public enum ScalingAxis
	{
		Width,
		Height
	}
	public ScalingAxis scalingAxis = ScalingAxis.Height;
	public enum Anchor
	{
		Left,
		Right
	}
	public Anchor anchor = Anchor.Left;
	public float activationRange = 1.0f;
	public bool customActivationRange = false;
	public float activationWidth = 50.0f, activationHeight = 75.0f;
	public float activationPositionHorizontal = 0.0f, activationPositionVertical = 0.0f;
	public float joystickSize = 2.5f, radiusModifier = 4.5f;
	public float positionHorizontal = 5.0f, positionVertical = 20.0f;

	// JOYSTICK SETTINGS //
	public bool dynamicPositioning = false;
	public float gravity = 60.0f;
	bool gravityActive = false;
	public bool extendRadius = false;
	public enum Axis
	{
		Both,
		X,
		Y
	}
	public Axis axis = Axis.Both;
	public enum Boundary
	{
		Circular,
		Square
	}
	public Boundary boundary = Boundary.Circular;
	public float deadZone = 0.0f;
	public enum TapCountOption
	{
		NoCount,
		Accumulate,
		TouchRelease
	}
	public TapCountOption tapCountOption = TapCountOption.NoCount;
	public float tapCountDuration = 0.5f;
	public int targetTapCount = 2;
	float currentTapTime = 0.0f;
	int tapCount = 0;
	public bool useTouchInput = false;

	// VISUAL OPTIONS //
	public bool disableVisuals = false;
	public bool inputTransition = false;
	public float transitionUntouchedDuration = 0.1f, transitionTouchedDuration = 0.1f;
	float transitionUntouchedSpeed, transitionTouchedSpeed;
	public bool useFade = false;
	public float fadeUntouched = 1.0f, fadeTouched = 0.5f;
	public bool useScale = false;
	public float scaleTouched = 0.9f;
	public bool showHighlight = false;
	public Color highlightColor = new Color( 1, 1, 1, 1 );
	public Image highlightBase, highlightJoystick;
	public bool showTension = false;
	public Color tensionColorNone = new Color( 1, 1, 1, 1 ), tensionColorFull = new Color( 1, 1, 1, 1 );
	public enum TensionType
	{
		Directional,
		Free
	}
	public TensionType tensionType = TensionType.Directional;
	public float rotationOffset = 0.0f;
	public float tensionDeadZone = 0.0f;
	public List<Image> TensionAccents = new List<Image>();
	
	// SCRIPT REFERENCE //
	static Dictionary<string,UltimateJoystick> UltimateJoysticks = new Dictionary<string, UltimateJoystick>();
	public string joystickName;
	bool joystickState = false;
	bool tapCountAchieved = false;

	// PUBLIC CALLBACKS //
	public event Action OnPointerDownCallback, OnPointerUpCallback, OnDragCallback;
	public event Action OnUpdatePositioning;
	
	// OBSOLETE // NOTE: We are keeping these variables in the script and public so that the values can be copied to the new variables for a smooth transition to the new version.
	public enum JoystickTouchSize
	{
		Default,
		Medium,
		Large,
		Custom
	}
	[Header( "Depreciated Variables" )]
	public JoystickTouchSize joystickTouchSize = JoystickTouchSize.Default;
	public float customSpacing_X = -10, customSpacing_Y = -10;
	public float customTouchSize_X = -10, customTouchSize_Y = -10;
	public float customTouchSizePos_X = -10, customTouchSizePos_Y = -10;
	public RectTransform joystickSizeFolder;
	public Image tensionAccentUp, tensionAccentDown;
	public Image tensionAccentLeft, tensionAccentRight;
	

	void OnEnable ()
	{
		// If the user wants to calculate using touch input, then start the coroutine to catch the input.
		if( Application.isPlaying && useTouchInput )
			StartCoroutine( ProcessTouchInput() );
	}

	void OnDisable ()
	{
		// If the users was wanting to use touch input, then stop the coroutine.
		if( Application.isPlaying && useTouchInput )
			StopCoroutine( ProcessTouchInput() );
	}

	void Awake ()
	{
		// If the game is not being run and the joystick name has been assigned...
		if( Application.isPlaying && joystickName != string.Empty )
		{
			// If the static dictionary has this joystick registered, then remove it from the list.
			if( UltimateJoysticks.ContainsKey( joystickName ) )
				UltimateJoysticks.Remove( joystickName );

			// Then register the joystick.
			UltimateJoysticks.Add( joystickName, this );
		}
	}

	void Start ()
	{
		// If the game is not running then return.
		if( !Application.isPlaying )
			return;
		
		// If the user wants to transition on different input...
		if( inputTransition )
		{
			// Try to store the canvas group.
			joystickGroup = GetComponent<CanvasGroup>();

			// If the canvas group is still null, then add a canvas group component.
			if( joystickGroup == null )
				joystickGroup = baseTrans.gameObject.AddComponent<CanvasGroup>();

			// Configure the transition speeds.
			transitionUntouchedSpeed = 1.0f / transitionUntouchedDuration;
			transitionTouchedSpeed = 1.0f / transitionTouchedDuration;
		}

		// If the parent canvas is null...
		if( ParentCanvas == null )
		{
			// Then try to get the parent canvas component.
			UpdateParentCanvas();

			// If it is still null, then log a error and return.
			if( ParentCanvas == null )
			{
				Debug.LogError( "Ultimate Joystick\nThis component is not with a Canvas object. Disabling this component to avoid any errors." );
				enabled = false;
				return;
			}
		}

		// If the parent canvas does not have a screen size updater, then add it.
		if( !ParentCanvas.GetComponent<UltimateJoystickScreenSizeUpdater>() )
			ParentCanvas.gameObject.AddComponent<UltimateJoystickScreenSizeUpdater>();

		// Update the size and placement of the joystick.
		UpdateJoystickPositioning();
	}

	// THIS IS FOR THE UNITY EVENT SYSTEM IF THE USER WANTS THAT //
	public void OnPointerDown ( PointerEventData touchInfo )
	{
		if( useTouchInput )
			return;

		ProcessOnInputDown( touchInfo.position, touchInfo.pointerId );
	}

	public void OnDrag ( PointerEventData touchInfo )
	{
		if( useTouchInput )
			return;

		ProcessOnInputMoved( touchInfo.position, touchInfo.pointerId );
	}

	public void OnPointerUp ( PointerEventData touchInfo )
	{
		if( useTouchInput )
			return;

		ProcessOnInputUp( touchInfo.position, touchInfo.pointerId );
	}
	// END FOR UNITY EVENT SYSTEM //

	/// <summary>
	/// The coroutine will process the touch input if the user has the useTouchInput boolean enabled.
	/// </summary>
	IEnumerator ProcessTouchInput ()
	{
		// Loop for as long as useTouchInput is true.
		while( useTouchInput )
		{
			// If there are touches on the screen...
			if( Input.touchCount > 0 )
			{
				// Loop through each finger on the screen...
				for( int fingerId = 0; fingerId < Input.touchCount; fingerId++ )
				{
					// If the input phase has begun...
					if( Input.GetTouch( fingerId ).phase == TouchPhase.Began )
					{
						// If the touch input position is within the bounds of the joystick rect, then process the down input on the joystick.
						if( joystickRect.Contains( Input.GetTouch( fingerId ).position ) )
							ProcessOnInputDown( Input.GetTouch( fingerId ).position, fingerId );
					}
					// Else if the input has moved, then process the moved input.
					else if( Input.GetTouch( fingerId ).phase == TouchPhase.Moved )
						ProcessOnInputMoved( Input.GetTouch( fingerId ).position, fingerId );
					// Else if the input has ended or if it was canceled, then process the input being released.
					else if( Input.GetTouch( fingerId ).phase == TouchPhase.Ended || Input.GetTouch( fingerId ).phase == TouchPhase.Canceled )
						ProcessOnInputUp( Input.GetTouch( fingerId ).position, fingerId );
				}
			}
			// Else there are no touches on the screen.
			else
			{
				// If the inputId is not reset then reset the joystick since there are no touches.
				if( _inputId > -10 )
					ResetJoystick();
			}
			
			yield return null;
		}
	}
	
	/// <summary>
	/// Processes the input when it has been initiated on the joystick.
	/// </summary>
	/// <param name="inputPosition">The position of the input on the screen.</param>
	/// <param name="inputId">The unique id of the input that has been initiated on the joystick.</param>
	void ProcessOnInputDown ( Vector2 inputPosition, int inputId )
	{
		// If the joystick is already in use, then return.
		if( joystickState )
			return;
		
		// If the user wants a circular boundary but does not want a custom activation range...
		if( boundary == Boundary.Circular && !customActivationRange )
		{
			// distance = distance between the world position of the joystickBase cast to a local position of the ParentCanvas (* by scale factor) - half of the actual canvas size, and the input position.
			float distance = Vector2.Distance( ( Vector2 )( ParentCanvas.transform.InverseTransformPoint( joystickBase.position ) * ParentCanvas.scaleFactor ) + ( ( canvasRectTrans.sizeDelta * ParentCanvas.scaleFactor ) / 2 ), inputPosition );

			// If the distance is out of range, then just return.
			if( distance / ( baseTrans.sizeDelta.x * ParentCanvas.scaleFactor ) > 0.5f )
				return;
		}
		
		// Set the joystick state since the joystick is being interacted with and assign the inputId so that the other functions can know if the pointer calling the function is the correct one.
		joystickState = true;
		_inputId = inputId;
		
		// If the user has gravity set and it's active then stop the current movement.
		if( gravity > 0 && gravityActive )
			StopCoroutine( "GravityHandler" );

		// If dynamicPositioning or disableVisuals are enabled...
		if( dynamicPositioning || disableVisuals )
		{
			// Then move the joystickBase to the position of the touch.
			joystickBase.localPosition = ( Vector2 )baseTrans.InverseTransformPoint( ParentCanvas.transform.TransformPoint( inputPosition / ParentCanvas.scaleFactor ) ) - ( canvasRectTrans.sizeDelta / 2 );

			// Set the joystick center so that the position can be calculated correctly.
			UpdateJoystickCenter();
		}

		// If the user wants to show the input transitions...
		if( inputTransition )
		{
			// If either of the transition durations are set to something other than 0, then start the coroutine to transition over time.
			if( transitionUntouchedDuration > 0 || transitionTouchedDuration > 0 )
				StartCoroutine( "InputTransition" );
			// Else the user does not want to transition over time.
			else
			{
				// So just apply the touched alpha value.
				if( useFade )
					joystickGroup.alpha = fadeTouched;

				// And apply the touched scale.
				if( useScale )
					joystickBase.localScale = Vector3.one * scaleTouched;
			}
		}

		// If the user is wanting to use any tap count...
		if( tapCountOption != TapCountOption.NoCount )
		{
			// If the user is accumulating taps...
			if( tapCountOption == TapCountOption.Accumulate )
			{
				// If the TapCountdown is not counting down...
				if( currentTapTime <= 0 )
				{
					// Set tapCount to 1 since this is the initial touch and start the TapCountdown.
					tapCount = 1;
					StartCoroutine( "TapCountdown" );
				}
				// Else the TapCountdown is currently counting down, so increase the current tapCount.
				else
					++tapCount;

				if( currentTapTime > 0 && tapCount >= targetTapCount )
				{
					// Set the current time to 0 to interrupt the coroutine.
					currentTapTime = 0;

					// Start the delay of the reference for one frame.
					StartCoroutine( "TapCountDelay" );
				}
			}
			// Else the user wants to touch and release, so start the TapCountdown timer.
			else
				StartCoroutine( "TapCountdown" );
		}

		// Call ProcessInput with the current input information.
		ProcessInput( inputPosition );

		// Notify any subscribers that the OnPointerDown function has been called.
		if( OnPointerDownCallback != null )
			OnPointerDownCallback();
	}

	/// <summary>
	/// Processes the input when it has been moved on the screen.
	/// </summary>
	/// <param name="inputPosition">The position of the input on the screen.</param>
	/// <param name="inputId">The unique id of the input being sent in to this function.</param>
	void ProcessOnInputMoved ( Vector2 inputPosition, int inputId )
	{
		// If the pointer event that is calling this function is not the same as the one that initiated the joystick, then return.
		if( inputId != _inputId )
			return;

		// Then call ProcessInput with the info with the current input information.
		ProcessInput( inputPosition );

		// Notify any subscribers that the OnDrag function has been called.
		if( OnDragCallback != null )
			OnDragCallback();
	}

	/// <summary>
	/// Processes the input when it has been released.
	/// </summary>
	/// <param name="inputPosition">The position of the input on the screen.</param>
	/// <param name="inputId">The unique id of the input being sent into this function.</param>
	void ProcessOnInputUp ( Vector2 inputPosition, int inputId )
	{
		// If the pointer event that is calling this function is not the same as the one that initiated the joystick, then return.
		if( inputId != _inputId )
			return;
		
		// Since the touch has lifted, set the state to false and reset the local pointerId.
		joystickState = false;
		_inputId = -10;

		// If dynamicPositioning, disableVisuals, or extendRadius are enabled...
		if( dynamicPositioning || disableVisuals || extendRadius )
		{
			// The joystickBase needs to be reset back to the default position.
			joystickBase.localPosition = defaultPos;

			// Reset the joystick center since the touch has been released.
			UpdateJoystickCenter();
		}

		// If the user has the gravity set to something more than 0 but less than 60, begin GravityHandler().
		if( gravity > 0 && gravity < 60 )
			StartCoroutine( "GravityHandler" );
		// Else the user doesn't want to apply a gravity effect to the joystick...
		else
		{
			// Reset the joystick's position back to center.
			joystick.localPosition = Vector3.zero;

			// If the user is wanting to show tension, then reset that here.
			if( showTension )
				TensionAccentReset();
		}
		
		// If the user wants an input transition, but the durations of both touched and untouched states are zero...
		if( inputTransition && ( transitionTouchedDuration <= 0 && transitionUntouchedDuration <= 0 ) )
		{
			// Then just apply the alpha.
			if( useFade )
				joystickGroup.alpha = fadeUntouched;

			// And reset the scale back to one.
			if( useScale )
				joystickBase.localScale = Vector3.one;
		}

		// If the user is wanting to use the TouchAndRelease tap count...
		if( tapCountOption == TapCountOption.TouchRelease )
		{
			// If the tapTime is still above zero, then start the delay function.
			if( currentTapTime > 0 )
				StartCoroutine( "TapCountDelay" );

			// Reset the current tap time to zero.
			currentTapTime = 0;
		}

		// Update the position values.
		UpdatePositionValues();

		// Notify any subscribers that the OnPointerUp function has been called.
		if( OnPointerUpCallback != null )
			OnPointerUpCallback();
	}

	/// <summary>
	/// Processes the input provided and moves the joystick accordingly.
	/// </summary>
	/// <param name="inputPosition">The current position of the input.</param>
	void ProcessInput ( Vector2 inputPosition )
	{
		// Create a new Vector2 to equal the vector from the current touch to the center of joystick.
		Vector2 tempVector = inputPosition - joystickCenter;
		
		// If the user wants only one axis, then zero out the opposite value.
		if( axis == Axis.X )
			tempVector.y = 0;
		else if( axis == Axis.Y )
			tempVector.x = 0;

		// If the user wants a circular boundary for the joystick, then clamp the magnitude by the radius.
		if( boundary == Boundary.Circular )
			tempVector = Vector2.ClampMagnitude( tempVector, radius * joystickBase.localScale.x );
		// Else the user wants a square boundary, so clamp X and Y individually.
		else
		{
			tempVector.x = Mathf.Clamp( tempVector.x, -radius * joystickBase.localScale.x, radius * joystickBase.localScale.x );
			tempVector.y = Mathf.Clamp( tempVector.y, -radius * joystickBase.localScale.x, radius * joystickBase.localScale.x );
		}

		// Apply the tempVector to the joystick's position.
		joystick.localPosition = ( tempVector / joystickBase.localScale.x ) / ParentCanvas.scaleFactor;
		
		// If the user wants to drag the joystick along with the touch...
		if( extendRadius )
		{
			// Store the position of the current touch.
			Vector3 currentTouchPosition = inputPosition;

			// If the user is using any axis option, then align the current touch position.
			if( axis != Axis.Both )
			{
				if( axis == Axis.X )
					currentTouchPosition.y = joystickCenter.y;
				else
					currentTouchPosition.x = joystickCenter.x;
			}
			// Then find the distance that the touch is from the center of the joystick.
			float touchDistance = Vector3.Distance( joystickCenter, currentTouchPosition );

			// If the touchDistance is greater than the set radius...
			if( touchDistance >= radius )
			{
				// Figure out the current position of the joystick.
				Vector2 joystickPosition = joystick.localPosition / radius;
				
				// Move the joystickBase in the direction that the joystick is, multiplied by the difference in distance of the max radius.
				joystickBase.localPosition += new Vector3( joystickPosition.x, joystickPosition.y, 0 ) * ( touchDistance - radius );

				// Reconfigure the joystick center since the joystick has now moved it's position.
				UpdateJoystickCenter();
			}
		}

		// Update the position values since the joystick has been updated.
		UpdatePositionValues();

		// If the user has showTension enabled, then display the Tension.
		if( showTension )
			TensionAccentDisplay();
	}

	/// <summary>
	/// This function is called by Unity when the parent of this transform changes.
	/// </summary>
	void OnTransformParentChanged ()
	{
		UpdateParentCanvas();
	}

	/// <summary>
	/// Updates the parent canvas if it has changed.
	/// </summary>
	public void UpdateParentCanvas ()
	{
		// Store the parent of this object.
		Transform parent = transform.parent;

		// If the parent is null, then just return.
		if( parent == null )
			return;

		// While the parent is assigned...
		while( parent != null )
		{
			// If the parent object has a Canvas component, then assign the ParentCanvas and transform.
			if( parent.transform.GetComponent<Canvas>() )
			{
				ParentCanvas = parent.transform.GetComponent<Canvas>();
				canvasRectTrans = ParentCanvas.GetComponent<RectTransform>();
				return;
			}

			// If the parent does not have a canvas, then store it's parent to loop again.
			parent = parent.transform.parent;
		}
	}

	/// <summary>
	/// This function updates the joystick's position on the screen.
	/// </summary>
	void UpdateJoystickPositioning ()
	{
		// If the parent canvas is null, then try to get the parent canvas component.
		if( ParentCanvas == null )
			UpdateParentCanvas();

		// If it is still null, then log a error and return.
		if( ParentCanvas == null )
		{
			Debug.LogError( "Ultimate Joystick\nThere is no parent canvas object. Please make sure that the Ultimate Joystick is placed within a canvas." );
			return;
		}

		// If any of the needed components are left unassigned, then inform the user and return.
		if( joystickBase == null )
		{
			if( Application.isPlaying )
				Debug.LogError( "Ultimate Joystick\nThere are some needed components that are not currently assigned. Please check the Assigned Variables section and be sure to assign all of the components." );

			return;
		}

		// Set the current reference size for scaling.
		float referenceSize = scalingAxis == ScalingAxis.Height ? canvasRectTrans.sizeDelta.y : canvasRectTrans.sizeDelta.x;
		
		// Configure the target size for the joystick graphic.
		float textureSize = referenceSize * ( joystickSize / 10 );

		// If baseTrans is null, store this object's RectTrans so that it can be positioned.
		if( baseTrans == null )
			baseTrans = GetComponent<RectTransform>();

		// Force the anchors and pivot so the joystick will function correctly. This is also needed here for older versions of the Ultimate Joystick that didn't use these rect transform settings.
		baseTrans.anchorMin = Vector2.zero;
		baseTrans.anchorMax = Vector2.zero;
		baseTrans.pivot = new Vector2( 0.5f, 0.5f );
		baseTrans.localScale = Vector3.one;

		// Set the anchors of the joystick base. It is important to have the anchors centered for calculations.
		joystickBase.anchorMin = new Vector2( 0.5f, 0.5f );
		joystickBase.anchorMax = new Vector2( 0.5f, 0.5f );
		joystickBase.pivot = new Vector2( 0.5f, 0.5f );
		
		// Configure the position that the user wants the joystick to be located.
		Vector2 joystickPosition = new Vector2( canvasRectTrans.sizeDelta.x * ( positionHorizontal / 100 ) - ( textureSize * ( positionHorizontal / 100 ) ) + ( textureSize / 2 ), canvasRectTrans.sizeDelta.y * ( positionVertical / 100 ) - ( textureSize * ( positionVertical / 100 ) ) + ( textureSize / 2 ) ) - ( canvasRectTrans.sizeDelta / 2 );

		if( anchor == Anchor.Right )
			joystickPosition.x = -joystickPosition.x;
		
		// If the user wants a custom touch size...
		if( customActivationRange )
		{
			// Apply the size of the custom activation range.
			baseTrans.sizeDelta = new Vector2( canvasRectTrans.sizeDelta.x * ( activationWidth / 100 ), canvasRectTrans.sizeDelta.y * ( activationHeight / 100 ) );
			
			// Apply the new position minus half the canvas position size.
			baseTrans.localPosition = new Vector2( canvasRectTrans.sizeDelta.x * ( activationPositionHorizontal / 100 ) - ( baseTrans.sizeDelta.x * ( activationPositionHorizontal / 100 ) ) + ( baseTrans.sizeDelta.x / 2 ), canvasRectTrans.sizeDelta.y * ( activationPositionVertical / 100 ) - ( baseTrans.sizeDelta.y * ( activationPositionVertical / 100 ) ) + ( baseTrans.sizeDelta.y / 2 ) ) - ( canvasRectTrans.sizeDelta / 2 );
			
			// Apply the size and position to the joystickBase.
			joystickBase.sizeDelta = new Vector2( textureSize, textureSize );
			joystickBase.localPosition = baseTrans.transform.InverseTransformPoint( ParentCanvas.transform.TransformPoint( joystickPosition ) );
		}
		else
		{
			// Apply the joystick size multiplied by the activation range.
			baseTrans.sizeDelta = new Vector2( textureSize, textureSize ) * activationRange;

			// Apply the imagePosition.
			baseTrans.localPosition = joystickPosition;

			// Apply the size and position to the joystickBase.
			joystickBase.sizeDelta = new Vector2( textureSize, textureSize );
			joystickBase.localPosition = Vector3.zero;
		}
		
		// If the options dictate that the default position needs to be stored, then store it here.
		if( dynamicPositioning || disableVisuals || extendRadius )
			defaultPos = joystickBase.localPosition;
			
		// Configure the size of the Ultimate Joystick's radius.
		radius = ( joystickBase.sizeDelta.x * ParentCanvas.scaleFactor ) * ( radiusModifier / 10 );

		// Update the joystick center so that reference positions can be configured correctly.
		UpdateJoystickCenter();

		// If the user wants to transition, and the joystickGroup is unassigned, find the CanvasGroup.
		if( inputTransition && joystickGroup == null )
		{
			joystickGroup = GetComponent<CanvasGroup>();
			if( joystickGroup == null )
				joystickGroup = gameObject.AddComponent<CanvasGroup>();
		}

		// If the user wants to use touch input...
		if( useTouchInput )
		{
			// Configure the actual size delta and position of the base trans regardless of the canvas scaler setting.
			Vector2 baseSizeDelta = baseTrans.sizeDelta * ParentCanvas.scaleFactor;
			Vector2 baseLocalPosition = baseTrans.localPosition * ParentCanvas.scaleFactor;

			// Calculate the rect of the base trans.
			joystickRect = new Rect( new Vector2( baseLocalPosition.x - ( baseSizeDelta.x / 2 ), baseLocalPosition.y - ( baseSizeDelta.y / 2 ) ) + ( ( canvasRectTrans.sizeDelta * ParentCanvas.scaleFactor ) / 2 ), baseSizeDelta );
		}
	}

	/// <summary>
	/// Updates the joystick center value.
	/// </summary>
	void UpdateJoystickCenter ()
	{
		joystickCenter = ( ( Vector2 )ParentCanvas.transform.InverseTransformPoint( joystickBase.position ) * ParentCanvas.scaleFactor ) + ( ( canvasRectTrans.sizeDelta * ParentCanvas.scaleFactor ) / 2 );
	}

	/// <summary>
	/// This function is called only when showTension is true, and only when the joystick is moving.
	/// </summary>
	void TensionAccentDisplay ()
	{
		// If the tension accent images are null, then inform the user and return.
		if( TensionAccents.Count == 0 )
		{
			Debug.LogError( "Ultimate Joystick\nThere are no tension accent images assigned. This could be happening for several reasons, but all of them should be fixable in the Ultimate Joystick inspector." );
			return;
		}

		// If the user wants to display directional tension...
		if( tensionType == TensionType.Directional )
		{
			// Calculate the joystick axis values.
			Vector2 joystickAxis = ( joystick.localPosition * ParentCanvas.scaleFactor ) / radius;

			// If the joystick is to the right...
			if( joystickAxis.x > 0 )
			{
				// Then lerp the color according to tension's X position.
				if( TensionAccents[ 3 ] != null )
					TensionAccents[ 3 ].color = Color.Lerp( tensionColorNone, tensionColorFull, joystickAxis.x <= tensionDeadZone ? 0 : ( joystickAxis.x - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );
				
				// If the opposite tension is not tensionColorNone, the make it so.
				if( TensionAccents[ 1 ] != null && TensionAccents[ 1 ].color != tensionColorNone )
					TensionAccents[ 1 ].color = tensionColorNone;
			}
			// Else the joystick is to the left...
			else
			{
				// Repeat above steps...
				if( TensionAccents[ 1 ] != null )
					TensionAccents[ 1 ].color = Color.Lerp( tensionColorNone, tensionColorFull, Mathf.Abs( joystickAxis.x ) <= tensionDeadZone ? 0 : ( Mathf.Abs( joystickAxis.x ) - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );
				if( TensionAccents[ 3 ] != null && TensionAccents[ 3 ].color != tensionColorNone )
					TensionAccents[ 3 ].color = tensionColorNone;
			}

			// If the joystick is up...
			if( joystickAxis.y > 0 )
			{
				// Then lerp the color according to tension's Y position.
				if( TensionAccents[ 0 ] != null )
					TensionAccents[ 0 ].color = Color.Lerp( tensionColorNone, tensionColorFull, joystickAxis.y <= tensionDeadZone ? 0 : ( joystickAxis.y - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );

				// If the opposite tension is not tensionColorNone, the make it so.
				if( TensionAccents[ 2 ] != null && TensionAccents[ 2 ].color != tensionColorNone )
					TensionAccents[ 2 ].color = tensionColorNone;
			}
			// Else the joystick is down...
			else
			{
				// Repeat above steps...
				if( TensionAccents[ 2 ] != null )
					TensionAccents[ 2 ].color = Color.Lerp( tensionColorNone, tensionColorFull, Mathf.Abs( joystickAxis.y ) <= tensionDeadZone ? 0 : ( Mathf.Abs( joystickAxis.y ) - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );
				if( TensionAccents[ 0 ] != null && TensionAccents[ 0 ].color != tensionColorNone )
					TensionAccents[ 0 ].color = tensionColorNone;
			}
		}
		// Else the user wants to display free tension...
		else
		{
			// If the first index tension is null, then inform the user and return to avoid errors.
			if( TensionAccents[ 0 ] == null )
			{
				Debug.LogError( "Ultimate Joystick\nThere are no tension accent images assigned. This could be happening for several reasons, but all of them should be fixable in the Ultimate Joystick inspector." );
				return;
			}

			// Store the distance for calculations.
			float distance = GetDistance();

			// Lerp the color according to the distance of the joystick from center.
			TensionAccents[ 0 ].color = Color.Lerp( tensionColorNone, tensionColorFull, distance <= tensionDeadZone ? 0 : ( distance - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );
			
			// Calculate the joystick axis values.
			Vector2 joystickAxis = joystick.localPosition / radius;
			
			// Rotate the tension transform to aim at the direction that the joystick is pointing.
			TensionAccents[ 0 ].transform.localRotation = Quaternion.Euler( 0, 0, ( Mathf.Atan2( joystickAxis.y, joystickAxis.x ) * Mathf.Rad2Deg ) + rotationOffset - 90 );
		}
	}
	
	/// <summary>
	/// This function resets the tension image's colors back to default.
	/// </summary>
	void TensionAccentReset ()
	{
		// Loop through each tension accent.
		for( int i = 0; i < TensionAccents.Count; i++ )
		{
			// If the tension accent is unassigned, then skip this index.
			if( TensionAccents[ i ] == null )
				continue;

			// Reset the color of this tension image back to no tension.
			TensionAccents[ i ].color = tensionColorNone;
		}

		// If the joystick is using a free tension, then reset the tension rotation back to center.
		if( tensionType == TensionType.Free && TensionAccents.Count > 0 && TensionAccents[ 0 ] != null )
			TensionAccents[ 0 ].transform.localRotation = Quaternion.identity;
	}
	
	/// <summary>
	/// This function is for returning the joystick back to center for a set amount of time.
	/// </summary>
	IEnumerator GravityHandler ()
	{
		// Set gravityActive to true so other functions know it is running.
		gravityActive = true;

		// Calculate the speed according to the distance left from center.
		float speed = 1.0f / ( GetDistance() / gravity );

		// Store the position of where the joystick is currently.
		Vector3 startJoyPos = joystick.localPosition;

		// Loop for the time it will take for the joystick to return to center.
		for( float t = 0.0f; t < 1.0f && gravityActive; t += Time.deltaTime * speed )
		{
			// Lerp the joystick's position from where this coroutine started to the center.
			joystick.localPosition = Vector3.Lerp( startJoyPos, Vector3.zero, t );

			// If the user a direction display option enabled, then display the direction as the joystick moves.
			if( showTension )
				TensionAccentDisplay();

			// Update the position values since the joystick has moved.
			UpdatePositionValues();

			yield return null;
		}

		// If the gravityActive controller is still true, then the user has not interrupted the joystick returning to center.
		if( gravityActive )
		{
			// Finalize the joystick's position.
			joystick.localPosition = Vector3.zero;

			// Here at the end, reset the direction display.
			if( showTension )
				TensionAccentReset();

			// And update the position values since the joystick has reached the center.
			UpdatePositionValues();
		}

		// Set gravityActive to false so that other functions can know it is finished.
		gravityActive = false;
	}

	/// <summary>
	/// This coroutine will handle the input transitions over time according to the users options.
	/// </summary>
	IEnumerator InputTransition ()
	{
		// Store the current values for the alpha and scale of the joystick.
		float currentAlpha = joystickGroup.alpha;
		float currentScale = joystickBase.localScale.x;

		// If the scaleInSpeed is NaN....
		if( float.IsInfinity( transitionTouchedSpeed ) )
		{
			// Set the alpha to the touched value.
			if( useFade )
				joystickGroup.alpha = fadeTouched;

			// Set the scale to the touched value.
			if( useScale )
				joystickBase.localScale = Vector3.one * scaleTouched;
		}
		// Else run the loop to transition to the desired values over time.
		else
		{
			// This for loop will continue for the transition duration.
			for( float transition = 0.0f; transition < 1.0f && joystickState; transition += Time.deltaTime * transitionTouchedSpeed )
			{
				// Lerp the alpha of the canvas group.
				if( useFade )
					joystickGroup.alpha = Mathf.Lerp( currentAlpha, fadeTouched, transition );

				// Lerp the scale of the joystick.
				if( useScale )
					joystickBase.localScale = Vector3.one * Mathf.Lerp( currentScale, scaleTouched, transition );

				yield return null;
			}

			// If the joystick is still being interacted with, then finalize the values since the loop above has ended.
			if( joystickState )
			{
				if( useFade )
					joystickGroup.alpha = fadeTouched;

				if( useScale )
					joystickBase.localScale = Vector3.one * scaleTouched;
			}
		}

		// While loop for while joystickState is true
		while( joystickState )
			yield return null;

		// Set the current values.
		currentAlpha = joystickGroup.alpha;
		currentScale = joystickBase.localScale.x;

		// If the scaleOutSpeed value is NaN, then apply the desired alpha and scale.
		if( float.IsInfinity( transitionUntouchedSpeed ) )
		{
			if( useFade )
				joystickGroup.alpha = fadeUntouched;

			if( useScale )
				joystickBase.localScale = Vector3.one;
		}
		// Else run the loop to transition to the desired values over time.
		else
		{
			for( float transition = 0.0f; transition < 1.0f && !joystickState; transition += Time.deltaTime * transitionUntouchedSpeed )
			{
				if( useFade )
					joystickGroup.alpha = Mathf.Lerp( currentAlpha, fadeUntouched, transition );

				if( useScale )
					joystickBase.localScale = Vector3.one * Mathf.Lerp( currentScale, 1.0f, transition );
				yield return null;
			}

			// If the joystick is still not being interacted with, then finalize the alpha and scale since the loop above finished.
			if( !joystickState )
			{
				if( useFade )
					joystickGroup.alpha = fadeUntouched;

				if( useScale )
					joystickBase.localScale = Vector3.one;
			}
		}
	}

	/// <summary>
	/// This function counts down the tap count duration. The current tap time that is being modified is check within the input functions.
	/// </summary>
	IEnumerator TapCountdown ()
	{
		// Set the current tap time to the max.
		currentTapTime = tapCountDuration;
		while( currentTapTime > 0 )
		{
			// Reduce the current time.
			currentTapTime -= Time.deltaTime;
			yield return null;
		}
	}

	/// <summary>
	/// This function delays for one frame so that it can be correctly referenced as soon as it is achieved.
	/// </summary>
	IEnumerator TapCountDelay ()
	{
		tapCountAchieved = true;
		yield return new WaitForEndOfFrame();
		tapCountAchieved = false;
	}
	
	/// <summary>
	/// This function updates the position values of the joystick so that they can be referenced.
	/// </summary>
	void UpdatePositionValues ()
	{
		// Store the relative position of the joystick and divide the Vector by the radius of the joystick. This will normalize the values.
		Vector2 joystickPosition = ( joystick.localPosition * ParentCanvas.scaleFactor ) / radius;

		// If the distance of the joystick from center is less that the dead zone set by the user...
		if( GetDistance() <= deadZone )
		{
			// Then zero out the axis values.
			joystickPosition.x = 0.0f;
			joystickPosition.y = 0.0f;
		}

		// Finally, set the horizontal and vertical axis values for reference.
		HorizontalAxis = joystickPosition.x;
		VerticalAxis = joystickPosition.y;
	}

	/// <summary>
	/// Returns with a confirmation about the existence of the targeted Ultimate Joystick.
	/// </summary>
	static bool JoystickConfirmed ( string joystickName )
	{
		// If the dictionary list doesn't contain this joystick name...
		if( !UltimateJoysticks.ContainsKey( joystickName ) )
		{
			// Log a warning to the user and return false.
			Debug.LogWarning( "Ultimate Joystick\nNo Ultimate Joystick has been registered with the name: " + joystickName + "." );
			return false;
		}

		// Return true because the dictionary does contain the joystick name.
		return true;
	}

	/// <summary>
	/// Resets the joystick position and input information and stops any coroutines that might have been running.
	/// </summary>
	void ResetJoystick ()
	{
		// Reset all of the controller variables.
		gravityActive = false;
		joystickState = false;
		_inputId = -10;

		// Stop the gravity coroutine.
		StopCoroutine( "GravityHandler" );
		
		// If dynamicPositioning, disableVisuals, or draggable are enabled...
		if( dynamicPositioning || disableVisuals || extendRadius )
		{
			// The joystickBase needs to be reset back to the default position.
			joystickBase.localPosition = defaultPos;

			// Reset the joystick center since the touch has been released.
			UpdateJoystickCenter();
		}

		// Reset the joystick's position back to center.
		joystick.localPosition = Vector3.zero;

		// Update the position values.
		UpdatePositionValues();

		// If the user has showTension enabled, then reset the tension.
		if( showTension )
			TensionAccentReset();
	}

	#if UNITY_EDITOR
	void Update ()
	{
		// Keep the joystick updated while the game is not being played.
		if( !Application.isPlaying )
			UpdateJoystickPositioning();
	}
	#endif

	/* --------------------------------------------- *** PUBLIC FUNCTIONS FOR THE USER *** --------------------------------------------- */
	/// <summary>
	/// Resets the joystick and updates the size and placement of the Ultimate Joystick. Useful for screen rotations, changing of screen size, or changing of size and placement options.
	/// </summary>
	public void UpdatePositioning ()
	{
		// If the game is running, then reset the joystick.
		if( Application.isPlaying )
			ResetJoystick();

		// Update the positioning.
		UpdateJoystickPositioning();

		// Notify any subscribers that the UpdatePositioning function has been called.
		if( OnUpdatePositioning != null )
			OnUpdatePositioning();
	}
	
	/// <summary>
	/// Returns a float value between -1 and 1 representing the horizontal value of the Ultimate Joystick.
	/// </summary>
	public float GetHorizontalAxis ()
	{
		return HorizontalAxis;
	}

	/// <summary>
	/// Returns a float value between -1 and 1 representing the vertical value of the Ultimate Joystick.
	/// </summary>
	public float GetVerticalAxis ()
	{
		return VerticalAxis;
	}

	/// <summary>
	/// Returns a value of -1, 0 or 1 representing the raw horizontal value of the Ultimate Joystick.
	/// </summary>
	public float GetHorizontalAxisRaw ()
	{
		float temp = HorizontalAxis;

		if( Mathf.Abs( temp ) <= deadZone )
			temp = 0.0f;
		else
			temp = temp < 0.0f ? -1.0f : 1.0f;

		return temp;
	}

	/// <summary>
	/// Returns a value of -1, 0 or 1 representing the raw vertical value of the Ultimate Joystick.
	/// </summary>
	public float GetVerticalAxisRaw ()
	{
		float temp = VerticalAxis;
		if( Mathf.Abs( temp ) <= deadZone )
			temp = 0.0f;
		else
			temp = temp < 0.0f ? -1.0f : 1.0f;

		return temp;
	}

	/// <summary>
	/// Returns the current value of the horizontal axis.
	/// </summary>
	public float HorizontalAxis
	{
		get;
		private set;
	}

	/// <summary>
	/// Returns the current value of the vertical axis.
	/// </summary>
	public float VerticalAxis
	{
		get;
		private set;
	}

	/// <summary>
	/// Returns a float value between 0 and 1 representing the distance of the joystick from the base.
	/// </summary>
	public float GetDistance ()
	{
		return Vector3.Distance( joystick.localPosition * ParentCanvas.scaleFactor, Vector3.zero ) / radius;
	}

	/// <summary>
	/// Updates the color of the highlights attached to the Ultimate Joystick with the targeted color.
	/// </summary>
	/// <param name="targetColor">New highlight color.</param>
	public void UpdateHighlightColor ( Color targetColor )
	{
		// If the user doesn't want to show highlight, then return.
		if( !showHighlight )
			return;

		// Assigned the new color.
		highlightColor = targetColor;
		
		// if the base highlight is assigned then apply the color.
		if( highlightBase != null )
			highlightBase.color = highlightColor;

		// If the joystick highlight image is assigned, apply the highlight color.
		if( highlightJoystick != null )
			highlightJoystick.color = highlightColor;
	}

	/// <summary>
	/// Updates the colors of the tension accents attached to the Ultimate Joystick with the targeted colors.
	/// </summary>
	/// <param name="targetTensionNone">New idle tension color.</param>
	/// <param name="targetTensionFull">New full tension color.</param>
	public void UpdateTensionColors ( Color targetTensionNone, Color targetTensionFull )
	{
		// If the user doesn't want to show tension, then just return.
		if( !showTension )
			return;

		// Assign the tension colors.
		tensionColorNone = targetTensionNone;
		tensionColorFull = targetTensionFull;
	}

	/// <summary>
	/// Returns the current state of the Ultimate Joystick. This function will return true when the joystick is being interacted with, and false when not.
	/// </summary>
	public bool GetJoystickState ()
	{
		return joystickState;
	}

	/// <summary>
	/// Returns the tap count to the Ultimate Joystick.
	/// </summary>
	public bool GetTapCount ()
	{
		return tapCountAchieved;
	}

	/// <summary>
	/// Disables the Ultimate Joystick.
	/// </summary>
	public void DisableJoystick ()
	{
		// Set the states to false.
		joystickState = false;
		_inputId = -10;
		
		// If the joystick center has been changed, then reset it.
		if( dynamicPositioning || disableVisuals || extendRadius )
		{
			joystickBase.localPosition = defaultPos;
			UpdateJoystickCenter();
		}
		
		// Reset the position of the joystick.
		joystick.localPosition = Vector3.zero;

		// Update the joystick position values since the joystick has been reset.
		UpdatePositionValues();
		
		// If the user is displaying tension accents, then reset them here.
		if( showTension )
			TensionAccentReset();

		// If the user wants to show a transition on the different input states...
		if( inputTransition )
		{
			// If the user is displaying a fade, then reset to the untouched state.
			if( useFade )
				joystickGroup.alpha = fadeUntouched;

			// If the user is scaling the joystick, then reset the scale.
			if( useScale )
				joystickBase.transform.localScale = Vector3.one;
		}
		
		// Disable the gameObject.
		gameObject.SetActive( false );
	}

	/// <summary>
	/// Enables the Ultimate Joystick.
	/// </summary>
	public void EnableJoystick ()
	{
		// Reset the joystick's position again.
		joystick.localPosition = Vector3.zero;

		// Enable the gameObject.
		gameObject.SetActive( true );
	}
	/* ------------------------------------------- *** END PUBLIC FUNCTIONS FOR THE USER *** ------------------------------------------- */
	
	/* --------------------------------------------- *** STATIC FUNCTIONS FOR THE USER *** --------------------------------------------- */
	/// <summary>
	/// Returns the Ultimate Joystick of the targeted name if it exists within the scene.
	/// </summary>
	/// <param name="joystickName">The Joystick Name of the desired Ultimate Joystick.</param>
	public static UltimateJoystick GetUltimateJoystick ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return null;

		return UltimateJoysticks[ joystickName ];
	}

	/// <summary>
	/// Returns a float value between -1 and 1 representing the horizontal value of the Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name of the desired Ultimate Joystick.</param>
	public static float GetHorizontalAxis ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetHorizontalAxis();
	}

	/// <summary>
	/// Returns a float value between -1 and 1 representing the vertical value of the Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name of the desired Ultimate Joystick.</param>
	public static float GetVerticalAxis ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetVerticalAxis();
	}

	/// <summary>
	/// Returns a value of -1, 0 or 1 representing the raw horizontal value of the Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name of the desired Ultimate Joystick.</param>
	public static float GetHorizontalAxisRaw ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetHorizontalAxisRaw();
	}

	/// <summary>
	/// Returns a value of -1, 0 or 1 representing the raw vertical value of the Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name of the desired Ultimate Joystick.</param>
	public static float GetVerticalAxisRaw ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetVerticalAxisRaw();
	}

	/// <summary>
	/// Returns a float value between 0 and 1 representing the distance of the joystick from the base.
	/// </summary>
	/// <param name="joystickName">The name of the desired Ultimate Joystick.</param>
	public static float GetDistance ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetDistance();
	}

	/// <summary>
	/// Returns the current interaction state of the Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name of the desired Ultimate Joystick.</param>
	public static bool GetJoystickState ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return false;

		return UltimateJoysticks[ joystickName ].joystickState;
	}

	/// <summary>
	/// Returns the current state of the tap count according to the options set.
	/// </summary>
	/// <param name="joystickName">The name of the desired Ultimate Joystick.</param>
	public static bool GetTapCount ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return false;

		return UltimateJoysticks[ joystickName ].GetTapCount();
	}

	/// <summary>
	/// Disables the targeted Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name of the desired Ultimate Joystick.</param>
	public static void DisableJoystick ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return;

		UltimateJoysticks[ joystickName ].DisableJoystick();
	}

	/// <summary>
	/// Enables the targeted Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name of the desired Ultimate Joystick.</param>
	public static void EnableJoystick ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return;

		UltimateJoysticks[ joystickName ].EnableJoystick();
	}
	/* ------------------------------------------- *** END STATIC FUNCTIONS FOR THE USER *** ------------------------------------------- */
}