using System;
using System.Collections;
using DenizYanar.Detections;
using DenizYanar.Events;
using DenizYanar.SenseEngine;
using DenizYanar.FSM;
using DenizYanar.PlayerInputSystem;
using DenizYanar.Movement;
using Sirenix.OdinInspector;
using UnityEngine;


namespace DenizYanar.PlayerSystem.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovementController : MonoBehaviour
    {
        #region Private Variables

        private Rigidbody2D m_Rb;
        private StateMachine m_StateMachine;

        private WallDetectionForCharacters m_WallDetectionForCharacters;
        private GroundDetection m_GroundDetection;

        private bool m_bHasJumpRequest;
        private bool m_HasLandCooldown;

        #endregion

        #region Private State Variables

        private IdleState m_sIdle;
        private MoveState m_sMove;
        private JumpState m_sJump;
        private TeleportState m_sTeleport;
        private AirState m_sAir;
        private LandState m_sLand;
        private WallSlideState m_sWallSlide;

        #endregion

        #region Serialized Variables

        [Header("Player Settings")]
        [SerializeField] [Required]
        private PlayerConfigurations m_Configurations;

        [Header("Player State Informer Channel")]
        [SerializeField] [Required]
        private StringEvent m_ecStateChangeTitle;

        [Header("Dependencies")]
        [SerializeField] [Required]
        private Collider2D m_PlayerCollision;

        [Header("Senses")]
        [SerializeField]
        private SenseEnginePlayer m_sepJump;

        [SerializeField]
        private SenseEnginePlayer m_sepLand;

        #endregion

        #region Public Variables

        public JumpData m_JumpDataInstance { get; private set; }
        public WallSlideData WallSlideDataInstance { get; private set; }

        #endregion

        #region Monobehaviour

        private void OnEnable()
        {
            PlayerInputs.e_OnJumpStarted += OnJumpStarted;
            PlayerInputs.e_OnAttack1Started += OnAttack1Started;
            
        }

        private void OnDisable()
        {
            PlayerInputs.e_OnJumpStarted -= OnJumpStarted;
            PlayerInputs.e_OnAttack1Started -= OnAttack1Started;
        }

        private void Awake()
        {
            m_Rb = GetComponent<Rigidbody2D>();
            SetupWallDetection();
            m_GroundDetection = new GroundDetection(m_PlayerCollision, 8, m_Configurations.ObstacleLayerMask);

            SetupStateMachine();
        }
        private void SetupWallDetection() => m_WallDetectionForCharacters = new WallDetectionForCharacters(m_PlayerCollision, 2, m_Configurations.ObstacleLayerMask);

        private void Update() => m_StateMachine.Tick();

        private void FixedUpdate() => m_StateMachine.PhysicsTick();

        #endregion

        #region Inputs

        private void OnJumpStarted() => StartCoroutine(RememberJumpRequest(0.15f));

        private void OnAttack1Started() => m_StateMachine.TriggerState(m_sTeleport);

        
        #endregion

        #region Local Methods

        private void SetupStateMachine()
        {
            m_JumpDataInstance = new JumpData(m_Configurations.JumpCount, m_Configurations.JumpForce, m_Rb);
            WallSlideDataInstance = new WallSlideData(m_Rb, m_PlayerCollision);
            
            var horizontalPhysicMovement = new HorizontalPhysicMovement(
                m_Rb,
                m_Configurations.AirStrafeMaxXVelocity,
                m_Configurations.AirStrafeXAcceleration
            );


            var verticalPhysicMovement = new VerticalPhysicMovement(
                m_Rb,
                m_Configurations.AirStrafeMaxYVelocity,
                m_Configurations.AirStrafeYAcceleration
            );

            m_StateMachine = new StateMachine();

            m_sIdle = new IdleState(m_Rb, nameInformerEvent: m_ecStateChangeTitle, stateName: "Idle");
            m_sMove = new MoveState(m_Rb, m_Configurations, nameInformerEvent: m_ecStateChangeTitle,  stateName: "Move");
            m_sJump = new JumpState(this, m_sepJump, nameInformerChannel: m_ecStateChangeTitle, stateName: "Jump");
            m_sLand = new LandState(m_JumpDataInstance, m_sepLand, nameInformerEvent: m_ecStateChangeTitle,    stateName: "Land");
            m_sWallSlide = new WallSlideState(this, m_Configurations, name: m_ecStateChangeTitle, stateName: "Wall Slide");
            m_sAir = new AirState(horizontalPhysicMovement, verticalPhysicMovement, nameInformerChannel: m_ecStateChangeTitle, stateName: "At Air");
            m_sTeleport = new TeleportState(m_Rb, m_Configurations);

            m_StateMachine.InitState(m_sIdle);

            To(m_sIdle, m_sMove, HasMovementInput());
            To(m_sMove, m_sIdle, HasNotMovementInput());
            To(m_sIdle, m_sJump, CanJump());
            To(m_sMove, m_sJump, CanJump());
            To(m_sIdle, m_sAir, NoMoreContactToGround());
            To(m_sMove, m_sAir, NoMoreContactToGround());
            To(m_sAir, m_sLand, OnFallToGround());
            To(m_sAir, m_sJump, CanJump());
            To(m_sLand, m_sIdle, AlwaysTrue());
            To(m_sAir, m_sWallSlide, OnContactToWall());
            To(m_sWallSlide, m_sAir, WhenJumpKeyTriggered());
            To(m_sWallSlide, m_sAir, NoContactToWall());
            To(m_sTeleport, m_sAir, OnSliceFinished());
            To(m_sJump, m_sAir, () => true);

            void To(State from, State to, Func<bool> condition) => m_StateMachine.AddTransition(@from, to, condition);

            Func<bool> HasMovementInput() => () => Mathf.Abs(PlayerInputs.m_HorizontalMovement) > 0;
            Func<bool> HasNotMovementInput() => () => PlayerInputs.m_HorizontalMovement == 0;
            Func<bool> CanJump() => () => m_bHasJumpRequest && m_JumpDataInstance.CanJump;
            Func<bool> WhenJumpKeyTriggered() => () => m_bHasJumpRequest;
            Func<bool> OnFallToGround() => IsLanded;
            Func<bool> NoMoreContactToGround() => () => !IsLanded();
            Func<bool> OnContactToWall() => () => IsTouchingToWall() && WallSlideDataInstance.HasCooldown == false;
            Func<bool> NoContactToWall() => () => !IsTouchingToWall();
            Func<bool> OnSliceFinished() => () => m_sTeleport.m_bHasFinished;
            Func<bool> AlwaysTrue() => () => true;
        }


        private bool IsLanded() => m_GroundDetection.IsTouchingGround() && !m_HasLandCooldown;

        private bool IsTouchingToWall() => m_WallDetectionForCharacters.DetectWall(PlayerInputs.m_HorizontalMovement * Vector2.right);

        public void StartLandCooldown() => StartCoroutine(LandCooldown(0.1f));
        

        private IEnumerator RememberJumpRequest(float duration)
        {
            if (m_bHasJumpRequest) yield break;
            
            m_bHasJumpRequest = true;
            yield return new WaitForSeconds(duration);
            m_bHasJumpRequest = false;
        }

        private IEnumerator LandCooldown(float duration)
        {
            if (m_HasLandCooldown) yield break;

            m_HasLandCooldown = true;
            yield return new WaitForSeconds(duration);
            m_HasLandCooldown = false;
        }

        #endregion
    }
}