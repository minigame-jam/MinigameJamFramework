using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Marginagility {
    [DefaultExecutionOrder(0)]
    public class MarginagilityPawn : Pawn {
        Vector2 _moveInput = Vector2.zero;
        [SerializeField]float _speed = 3f;
        [SerializeField]float _direction = 1f;
        [SerializeField]float _jumpRate = 1f;
        [SerializeField]float _interpRate = 0.05f;

        [SerializeField] Transform _lanceHitboxTransform;
        [SerializeField] Transform _lanceTransform;
        [SerializeField] Transform _lanceSpriteTransform;
        [SerializeField] Transform _rabbitTransform;
        [SerializeField] Transform _rabbitSpriteTransform;
        [SerializeField] Transform _arrowSpriteTransform;
        [SerializeField] Transform _frogTransform;
        [SerializeField] Transform _frogSpriteTransform;

        [SerializeField] Transform _lanceTargetTransform;
        [SerializeField] Transform _rabbitTargetTransform;
        [SerializeField] Transform _frogTargetTransform;


        Vector3 _lastRabbitPos;
        Vector3 _lastLancePos;
        Vector3 _lastLanceRotation;
        Vector3 _lastFrogPos;

        Vector3 _targetRabbitPos;
        Vector3 _targetLancePos;
        Vector3 _targetLanceRotation;
        Vector3 _targetFrogPos;

        [SerializeField]int _lane;
        [SerializeField]bool _hasLance = true;
        [SerializeField]bool _canDisarm = false;
        [SerializeField]int _lanceLane;

        [SerializeField]bool _isRiding = false;
        [SerializeField]bool _canRespawn = true;

        [SerializeField] SpriteRenderer _frogSprite;
        [SerializeField] SpriteRenderer _rabbitSprite;
        [SerializeField] SpriteRenderer _lanceSprite;
        [SerializeField] SpriteRenderer _arrowSprite;

        bool _scoreInitialized = false;

        float _respawnTimer = 5.0f;

        [SerializeField] GameObject _rabbitPrefab;
        [SerializeField] GameObject _lancePrefab;

        [SerializeField] float _moveDelay = 0.5f;
        [SerializeField] float _moveLanceDelay = 0.5f;
        float _moveTimeLast = 100f;
        float _moveLanceTimeLast = 100f;

        void Start() {
            _scoreInitialized = false;
            Despawn();
        }
        
        void Update() {
            if (FindAnyObjectByType<MarginagilityGameManager>().isGameOver()) return;

            float timer = FindAnyObjectByType<MarginagilityGameManager>().GetTimer();
            _speed = (Mathf.Sin(timer * _jumpRate) + 1) * 1.5f;

            if (!_scoreInitialized) {
                FindAnyObjectByType<ScoreDisplay>().SetScore(playerIndex, 0, true);
                _scoreInitialized = true;
            }

            if (playerIndex == -1) {
                Destroy(this.gameObject);
                Destroy(_lanceTransform.gameObject);
                Destroy(_rabbitTransform.gameObject);
                Destroy(_frogTransform.gameObject);
            }
            
            if (_isRiding) {
                JoustUpdate();
            } else if (_canRespawn) {
                SpawnSelectUpdate();
            } else {
                RespawnUpdate();
            }

        }

        void JoustUpdate() {

            transform.Translate(_speed * _direction * Time.deltaTime, 0, 0);

            if (transform.position.x > 7.5 || transform.position.x < -7.5) {
                Despawn();
                return;
            }

            _targetRabbitPos = _rabbitTargetTransform.position;
            _targetFrogPos = _frogTargetTransform.position;
            _targetLancePos = _lanceTargetTransform.position;
            _targetLanceRotation = _lanceTargetTransform.rotation.eulerAngles;

            if (_targetLanceRotation.z > 180) _targetLanceRotation.z -= 360;

            _rabbitTransform.position = _lastRabbitPos * (1-_interpRate) + _targetRabbitPos * _interpRate;
            _frogTransform.position = _lastFrogPos * (1-_interpRate) + _targetFrogPos * _interpRate;
            _lanceTransform.position = _lastLancePos * (1-_interpRate) + _targetLancePos * _interpRate;
            _lanceTransform.rotation = Quaternion.Euler(_lastLanceRotation * (1-_interpRate) + _targetLanceRotation * _interpRate);

            if (_moveTimeLast - _moveDelay >= FindAnyObjectByType<MarginagilityGameManager>().GetTimer()) {
                if (_moveInput.y > 0 && _lane != 1) {
                    transform.position = new Vector3(transform.position.x, 2, transform.position.z);
                    _lane = 1;
                    _moveTimeLast = FindAnyObjectByType<MarginagilityGameManager>().GetTimer();
                } else if (_moveInput.y < 0 && _lane != 0) {
                    transform.position = new Vector3(transform.position.x, -1, transform.position.z);
                    _lane = 0;
                    _moveTimeLast = FindAnyObjectByType<MarginagilityGameManager>().GetTimer();
                }
            }

            if (_hasLance) {
                if (_lane == _lanceLane) {
                    _lanceTargetTransform.localPosition = new Vector3(2.25f * _direction, 0.5f, 1.0f);
                    _lanceTargetTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));

                    _lanceHitboxTransform.localPosition = new Vector3(2.5f * _direction, 0.0f, 0.0f);
                    _lanceHitboxTransform.localScale = new Vector3(3.0f, 1.0f, 1.0f);
                } else if (_lane > _lanceLane) {
                    _lanceTargetTransform.localPosition = new Vector3(1.6f * _direction, -1.1f, 1.0f);
                    _lanceTargetTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, -45.0f * _direction));

                    _lanceHitboxTransform.localPosition = new Vector3(3.0f * _direction, -3.0f, 0.0f);
                    _lanceHitboxTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                } else if (_lane < _lanceLane) {
                    _lanceTargetTransform.localPosition = new Vector3(1.6f * _direction, 1.9f, 1.0f);
                    _lanceTargetTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, 45.0f * _direction));

                    _lanceHitboxTransform.localPosition = new Vector3(3.0f * _direction, 3.0f, 0.0f);
                    _lanceHitboxTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                }
            }

            _lastRabbitPos = _rabbitTransform.position;
            _lastLancePos = _lanceTransform.position;
            _lastLanceRotation = _lanceTransform.rotation.eulerAngles;
            if (_lastLanceRotation.z > 180) _lastLanceRotation.z -= 360;
            _lastFrogPos = _frogTransform.position;
        }

        void SpawnSelectUpdate() {

            if (_moveInput.y > 0) {
                _lane = 1;

            } else if (_moveInput.y < 0) {
                _lane = 0;
            }

            if (_moveInput.x > 0) {
                _direction = -1.0f;
            } else if (_moveInput.x < 0) {
                _direction = 1.0f;
            }

            Vector3 pos = Vector3.zero;
            pos.x = _direction == 1.0f ? -7.5f : 7.5f;
            pos.y = _lane == 1 ? 2 : -1;
            transform.position = pos;


            _respawnTimer -= Time.deltaTime;
            FindAnyObjectByType<ScoreDisplay>().SetRespawn(playerIndex, _respawnTimer);
            if (_respawnTimer <= 0.0f) {
                Respawn();
            }

            switch (playerIndex)
            {
                case 0:
                    _arrowSpriteTransform.localPosition = new Vector3(-0.5f, 0.0f, 0.0f);
                    _arrowSpriteTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    break;
                case 1:
                    _arrowSpriteTransform.localPosition = new Vector3(0.5f, 0.0f, 0.0f);
                    _arrowSpriteTransform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
                    break;
                case 2:
                    _arrowSpriteTransform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
                    _arrowSpriteTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    break;
                case 3:
                    _arrowSpriteTransform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
                    _arrowSpriteTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    break;

                default:
                    break;
            }
        }

        void RespawnUpdate() {

            transform.Translate(_speed * _direction * Time.deltaTime, 0, 0);

            if (transform.position.x > 7.5 || transform.position.x < -7.5) {
                Despawn();
                return;
            }

            _targetFrogPos = _frogTargetTransform.position;
            _frogTransform.position = _lastFrogPos * (1-_interpRate) + _targetFrogPos * _interpRate;
            _lastFrogPos = _frogTransform.position;

        }

        void Despawn() {
            _canRespawn = true;
            _isRiding = false;

            _frogSprite.enabled = false;
            _rabbitSprite.enabled = false;
            _lanceSprite.enabled = false;

            _arrowSprite.enabled = true;

            _respawnTimer = 5.0f;
        }

        void Respawn() {
            _canRespawn = false;
            _isRiding = true;
            _hasLance = true;

            _frogSprite.enabled = true;
            _rabbitSprite.enabled = true;
            _lanceSprite.enabled = true;

            _lanceTargetTransform.localPosition = new Vector3(2.25f * _direction, 0.5f, 1.0f);
            _lanceTargetTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));

            _targetRabbitPos = _rabbitTargetTransform.position;
            _targetFrogPos = _frogTargetTransform.position;
            _targetLancePos = _lanceTargetTransform.position;
            _targetLanceRotation = _lanceTargetTransform.rotation.eulerAngles;

            _rabbitTransform.position = _targetRabbitPos;
            _frogTransform.position = _targetFrogPos;
            _lanceTransform.position = _targetLancePos;
            _lanceTransform.rotation = Quaternion.Euler(_targetLanceRotation);

            _lastRabbitPos = _rabbitTransform.position;
            _lastLancePos = _lanceTransform.position;
            _lastLanceRotation = _lanceTransform.rotation.eulerAngles;
            if (_lastLanceRotation.z > 180) _lastLanceRotation.z -= 360;
            _lastFrogPos = _frogTransform.position;

            _rabbitSpriteTransform.localScale = new Vector3(_direction, 1.0f, 1.0f);
            _lanceSpriteTransform.localScale = new Vector3(_direction, 1.0f, 1.0f);
            _frogSpriteTransform.localScale = new Vector3(_direction, 1.0f, 1.0f);

            _arrowSprite.enabled = false;

            if (_lane == 1) {
                transform.position = new Vector3(-7.5f * _direction, 2, 0);
            } else {
                transform.position = new Vector3(-7.5f * _direction, -1, 0);
            }

            _lanceLane = _lane;

            FindAnyObjectByType<ScoreDisplay>().SetRespawn(playerIndex, 0.0f);
            FindAnyObjectByType<MarginagilityGameManager>().PlayRespawn();
        }

        protected override void OnActionPressed(InputAction.CallbackContext context) {
            if (FindAnyObjectByType<MarginagilityGameManager>().isGameOver()) return;

            if (context.action.name == PawnAction.Move) {
                _moveInput = context.ReadValue<Vector2>();
            }

            if (context.action.name == PawnAction.ButtonA) {
                if (_hasLance && _moveLanceTimeLast - _moveLanceDelay >= FindAnyObjectByType<MarginagilityGameManager>().GetTimer()) {
                    _lanceLane = (_lanceLane + 1) % 2;
                    _canDisarm = true;
                    _moveLanceTimeLast = FindAnyObjectByType<MarginagilityGameManager>().GetTimer();
                }
                if (_canRespawn) Respawn();
            }
        }

        public float GetDirection() {
            return _direction;
        }

        public float GetLane() {
            return _lane;
        }

        public float GetLanceLane() {
            return _lanceLane;
        }

        public bool GetRiding() {
            return _isRiding;
        }

        public bool GetLance() {
            return _hasLance;
        }

        public bool GetDisarm() {
            return _canDisarm;
        }

        public void UnsetDisarm() {
            _canDisarm = false;
        }

        public BoxCollider2D GetLanceHitbox() {
            return _lanceHitboxTransform.GetComponent<BoxCollider2D>();
        }

        public void Fall() {
            FindAnyObjectByType<ScoreDisplay>().AddToScore(playerIndex, -1);

            _isRiding = false;
            FallenObject fallenRabbit = Instantiate(_rabbitPrefab, 
                                                    _rabbitSpriteTransform.position, 
                                                    _rabbitSpriteTransform.rotation
                                            ).GetComponent<FallenObject>();
            fallenRabbit.GetSprite().color = _rabbitSprite.color;

            if (_hasLance) {
                FallenObject droppedLance = Instantiate(_lancePrefab, 
                                                    _lanceSpriteTransform.position, 
                                                    _lanceSpriteTransform.rotation
                                            ).GetComponent<FallenObject>();
                droppedLance.GetSprite().color = _lanceSprite.color;
            }
            _hasLance = false;

            _rabbitSprite.enabled = false;
            _lanceSprite.enabled = false;
        }

        public void DropLance() {
            _hasLance = false;

            _lanceSprite.enabled = false;
            FallenObject droppedLance = Instantiate(_lancePrefab, 
                                                    _lanceSpriteTransform.position, 
                                                    _lanceSpriteTransform.rotation
                                            ).GetComponent<FallenObject>();
            droppedLance.GetSprite().color = _lanceSprite.color;
        }

        public void Hit() {
            FindAnyObjectByType<ScoreDisplay>().AddToScore(playerIndex, 2);
        }

    }
}