namespace StreetCat.Data
{
    /// <summary>Chapter 1 intel / material / flag IDs aligned with design docs.</summary>
    public static class IntelIds
    {
        // Investigation
        public const string FixedFeedingPoint = "FIXED_FEEDING_POINT";
        public const string DafuRestSpot = "DAFU_REST_SPOT";
        public const string DafuAppearTime = "DAFU_APPEAR_TIME";
        public const string DafuNearGuard = "DAFU_NEAR_GUARD";
        public const string DafuBecameGuardCat = "DAFU_BECAME_GUARD_CAT";
        public const string DafuWasRescued = "DAFU_WAS_RESCUED";
        public const string DafuNoOwner = "DAFU_NO_OWNER";
        public const string CommunityCare = "COMMUNITY_CARE";

        // Da Fu interview
        public const string PastAfraid = "PAST_AFRAID";
        public const string NeckPain = "NECK_PAIN";
        public const string NeckObject = "NECK_OBJECT";
        public const string NeckObjectTight = "NECK_OBJECT_TIGHT";
        public const string NeckLongTermPain = "NECK_LONG_TERM_PAIN";
        public const string RepeatedFeeding = "REPEATED_FEEDING";
        public const string CaptureParticipant = "CAPTURE_PARTICIPANT";
        public const string TakenAway = "TAKEN_AWAY";
        public const string BrightStrangePlace = "BRIGHT_STRANGE_PLACE";
        public const string Sleep = "SLEEP";
        public const string ObjectGone = "OBJECT_GONE";
        public const string ReturnedDafu = "RETURNED_DAFU";
        public const string CognitiveBoundary = "COGNITIVE_BOUNDARY";
        public const string WomanClue = "WOMAN_CLUE";
        public const string TabbyPartner = "TABBY_PARTNER";

        // Lin interview
        public const string RopeEmbedded = "ROPE_EMBEDDED";
        public const string FeedFourDays = "FEED_FOUR_DAYS";
        public const string CaptureSuccess = "CAPTURE_SUCCESS";
        public const string PanleukopeniaDay3 = "PANLEUKOPENIA_DAY3";
        public const string TotalCost = "TOTAL_COST";
        public const string LinHesitated = "LIN_HESITATED";
        public const string FourCatsHome = "FOUR_CATS_HOME";
        public const string CannotFifth = "CANNOT_FIFTH";
        public const string ReturnOriginalArea = "RETURN_ORIGINAL_AREA";
        public const string CauseUnknown = "CAUSE_UNKNOWN";
        public const string LinIdentity = "LIN_IDENTITY";
    }

    public static class MaterialIds
    {
        public const string M01 = "M01";
        public const string M02 = "M02";
        public const string M03 = "M03";
        public const string M04 = "M04";
        public const string M05 = "M05";
        public const string M06 = "M06";
        public const string M07 = "M07";
        public const string M08 = "M08";
        public const string M09 = "M09";
        public const string M10 = "M10";
        public const string M11 = "M11";
        public const string M12 = "M12";
        public const string M13 = "M13";
        public const string M14 = "M14";
        public const string M15 = "M15";
        public const string M16 = "M16";
    }

    public static class FlagIds
    {
        public const string HasTranslator = "HAS_TRANSLATOR";
        public const string UnlockedHuaiAn = "UNLOCKED_HUAIAN";
        public const string FoundDafu = "FOUND_DAFU";
        public const string GuardUnlocked = "GUARD_UNLOCKED";
        public const string WaitingForDafu = "WAITING_FOR_DAFU";
        public const string DafuInterviewDone = "DAFU_INTERVIEW_DONE";
        public const string LinUnlocked = "LIN_UNLOCKED";
        public const string LinInterviewDone = "LIN_INTERVIEW_DONE";
        public const string WritingUnlocked = "WRITING_UNLOCKED";
        /// <summary>SC-10 intro dialogue finished; writing desk UI may open directly.</summary>
        public const string WritingDeskReady = "WRITING_DESK_READY";
        public const string ArticlePublished = "ARTICLE_PUBLISHED";
        public const string Chapter1Complete = "CHAPTER1_COMPLETE";
        public const string InvestigateTutorialShown = "INVESTIGATE_TUTORIAL";
    }

    public static class SceneIds
    {
        public const string SC01 = "SC-01";
        public const string SC02 = "SC-02";
        public const string SC03 = "SC-03";
        public const string SC04 = "SC-04";
        public const string SC05 = "SC-05";
        public const string SC06 = "SC-06";
        public const string SC07 = "SC-07";
        public const string SC08 = "SC-08";
        public const string SC09 = "SC-09";
        public const string SC10 = "SC-10";
        public const string SC11 = "SC-11";
    }

    public enum TopicStatus
    {
        Untouched = 0,
        Partial = 1,
        Deep = 2
    }

    public enum MaterialType
    {
        Fact,
        Detail,
        Emotion,
        Unconfirmed
    }

    public enum ArticleStage
    {
        A_PresentLife,
        B_PastInjury,
        C_RescueTreatment,
        D_Release,
        E_AfterReturn
    }

    public enum WritingDirection
    {
        GuardCatToday = 0,
        RescueWithoutAdoption = 1
    }

    public enum InterviewSubject
    {
        None,
        Dafu,
        Lin
    }

    public enum FactSource
    {
        DirectObservation,
        VetInformation,
        PersonalDecision,
        CommunityObservation,
        Uncertain,
        CatMemory
    }
}
