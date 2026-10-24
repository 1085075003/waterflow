using System.Runtime.CompilerServices;
using UnityEngine;

namespace com.google.game
{
	public class GoogleGame
	{
		public delegate void GameEventHandler(string eventName, string msg);

		public delegate void SnapshotOpenHandler(string snapshotmeta, byte[] data);

		public delegate void RTgameEventHandler(string eventName, string msg);

		private class GoogleGameListener : IGoogleGameListener
		{
			internal GoogleGame gameInstance;

			public void onGameEvent(string eventName, string paramString)
			{
			}

			public void onGetSnapshotData(string snapshotMeta, AndroidJavaObject snapshotData)
			{
			}
		}

		private class RealTimeMessageListener : IRealTimeMessageListener
		{
			internal GoogleGame gameInstance;

			public void onGameEvent(string eventName, string paramString)
			{
			}
		}

		[CompilerGenerated]
		private GameEventHandler m_gameEventHandler;

		[CompilerGenerated]
		private SnapshotOpenHandler m_snapshotDataHandler;

		[CompilerGenerated]
		private RTgameEventHandler m_rtgameEventHandler;

		private static GoogleGame _instance;

		private AndroidJavaObject jgame;

		public event GameEventHandler gameEventHandler
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event SnapshotOpenHandler snapshotDataHandler
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event RTgameEventHandler rtgameEventHandler
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public static GoogleGame Instance()
		{
			return null;
		}

		private void preInitGame()
		{
		}

		public void registerInvitationListener()
		{
		}

		public void registerTurnBasedMatchUpdateListener()
		{
		}

		public void loadPlayerInfo()
		{
		}

		public void getActivationHint()
		{
		}

		public void showInvitationInbox()
		{
		}

		public void unlockAchievement(string achievementId)
		{
		}

		public void revealAchievement(string achievementId)
		{
		}

		public void incrementAchievement(string achievementId, int step)
		{
		}

		public void setAchievementStep(string achievementId, int step)
		{
		}

		public void showAchievements()
		{
		}

		public void loadAchievements(bool forceReload)
		{
		}

		public void submitLeaderboardScore(string leaderboardID, long score)
		{
		}

		public void showLeaderboard(string leaderboardID)
		{
		}

		public void showLeaderboards()
		{
		}

		public void loadLeaderboardMetadata(string leaderboardID, bool forceReload)
		{
		}

		public void loadLeaderboardMetadatas(bool forceReload)
		{
		}

		public void loadTopScores(string leaderboardId, int span, int leaderboardCollection, int maxResults, bool forceReload)
		{
		}

		public void loadMoreTopScores(int maxResults, int pageDirection)
		{
		}

		public void loadMorePlayerCenteredScores(int maxResults, int pageDirection)
		{
		}

		public void loadPlayerCenteredScores(string leaderboardId, int span, int leaderboardCollection, int maxResults, bool forceReload)
		{
		}

		public void loadCurrentPlayerLeaderboardScore(string leaderboardId, int span, int leaderboardCollection)
		{
		}

		public void showSnapshot(string title, bool allowAddButton, bool allowDelete, int maxSnapshots)
		{
		}

		public void loadSnapshotMetas(bool forceReload)
		{
		}

		public void saveSnapshot(string uniqueName, byte[] screenShotImage, byte[] gameContent)
		{
		}

		public void deleteSnapshot(string uniqueName)
		{
		}

		public void getSnapshotContent(string uniqueName)
		{
		}

		public void showSelectRTOpponentsBox(int minAutoMatchPlayers, int maxAutoMatchPlayers, long exclusiveBitMask, bool allowAutomatch)
		{
		}

		public void createRTGameRoom(int minAutoMatchPlayers, int maxAutoMatchPlayers, long exclusiveBitMask, string[] invitees)
		{
		}

		public void leaveRTRoom(string roomID)
		{
		}

		public void showRTWaitingRoom(string roomID, int maxPlayerToStartGame)
		{
		}

		public void acceptRTInvite(string invitationId)
		{
		}

		public void dismissRTInvitation(string invitationId)
		{
		}

		public void declineRTInvitation(string invitationId)
		{
		}

		public void loadRTInvitations()
		{
		}

		public void sendReliableMessage(byte[] messageData, string roomId, string recipientParticipantId)
		{
		}

		public void sendUnreliableMessage(byte[] messageData, string roomId, string recipientParticipantId)
		{
		}

		public void sendUnreliableMessages(byte[] messageData, string roomId, string[] recipientParticipantIds)
		{
		}

		public void sendUnreliableMessageToOthers(byte[] messageData, string roomId)
		{
		}

		public void showSelectTurnbasedOpponentsBox(int minAutoMatchPlayers, int maxAutoMatchPlayers, bool allowAutomatch)
		{
		}

		public void createTurnbasedMatch(int minAutoMatchPlayers, int maxAutoMatchPlayers, string[] invitees)
		{
		}

		public void cancelTurnBasedMatch(string matchId)
		{
		}

		public void dismissTurnBasedMatch(string matchId)
		{
		}

		public void finishTurnbasedMatch(string matchId, byte[] matchData)
		{
		}

		public void leaveTurnBasedMatch(string matchId)
		{
		}

		public void leaveMatchDuringTurn(string matchId, string pendingParticipantId)
		{
		}

		public void loadTurnBasedMatch(string matchId)
		{
		}

		public void loadTurnBasedMatches(int invitationSortOrder, int[] matchTurnStatuses)
		{
		}

		public void rematchTurnBased(string matchId)
		{
		}

		public void takeTurn(string matchId, byte[] matchData, string pendingParticipantId)
		{
		}

		public void showTurnbasedInvitationInbox()
		{
		}

		public void acceptTurnBasedInvitation(string invitationId)
		{
		}

		public void declineTurnBasedInvitation(string invitationId)
		{
		}

		public void dismissTurnBasedInvitation(string invitationId)
		{
		}

		public void incrementEvent(string eventId, int incrementAmount)
		{
		}

		public void loadEvent(bool forceReload)
		{
		}

		public void loadEventByIds(bool forceReload, string[] eventIds)
		{
		}

		public void loadGameStates(bool forceReload)
		{
		}
	}
}
