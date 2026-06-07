-- Vamserlike MySQL schema
-- Azure MySQL / AWS RDS MySQL 양쪽에서 동일하게 사용할 기본 스키마
-- 기존 DynamoDB의 PlayerProfile 데이터를 MySQL 테이블로 전환한 구조

CREATE DATABASE IF NOT EXISTS vamserlike
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE vamserlike;

CREATE TABLE IF NOT EXISTS player_profiles
(
    -- Cognito 사용자 식별자
    user_id VARCHAR(128) NOT NULL,

    -- 로그인 이메일
    email VARCHAR(320) NOT NULL DEFAULT '',

    -- 게임 닉네임
    nickname VARCHAR(64) NOT NULL DEFAULT 'guest',

    -- 현재 선택 캐릭터
    selected_character_id VARCHAR(64) NOT NULL DEFAULT '',

    -- 마지막 플레이 캐릭터
    last_played_character_id VARCHAR(64) NOT NULL DEFAULT '',

    -- 보유 골드
    gold INT NOT NULL DEFAULT 0,

    -- 최고 점수
    best_score INT NOT NULL DEFAULT 0,

    -- 최고 레벨
    highest_level INT NOT NULL DEFAULT 0,

    -- 총 플레이 횟수
    total_play_count INT NOT NULL DEFAULT 0,

    -- 누적 적 처치 수
    total_kill_count INT NOT NULL DEFAULT 0,

    -- 해금된 캐릭터 목록
    -- MySQL에서는 List<string>을 JSON 문자열로 저장
    unlocked_character_ids LONGTEXT NOT NULL,

    -- 마지막 수정 시각 UTC
    updated_at_utc DATETIME(6) NOT NULL,

    PRIMARY KEY (user_id),

    INDEX idx_best_score (best_score),
    INDEX idx_total_kill_count (total_kill_count)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_unicode_ci;
