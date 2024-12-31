using GraphQL.Types;

namespace ArenaService;

public class ArenaParticipantType : ObjectGraphType<ArenaParticipantStruct>
{
    public ArenaParticipantType()
    {
        Field<NonNullGraphType<AddressType>>(
            nameof(ArenaParticipantStruct.AvatarAddr),
            description: "Address of avatar.",
            resolve: context => context.Source.AvatarAddr);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipantStruct.Score),
            description: "Arena score of avatar.",
            resolve: context => context.Source.Score);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipantStruct.Rank),
            description: "Arena rank of avatar.",
            resolve: context => context.Source.Rank);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipantStruct.WinScore),
            description: "Score for victory.",
            resolve: context => context.Source.WinScore);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipantStruct.LoseScore),
            description: "Score for defeat.",
            resolve: context => context.Source.LoseScore);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipantStruct.Cp),
            description: "Cp of avatar.",
            resolve: context => context.Source.Cp);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipantStruct.PortraitId),
            description: "Portrait icon id.",
            resolve: context => context.Source.PortraitId);
        Field<NonNullGraphType<IntGraphType>>(
            nameof(ArenaParticipantStruct.Level),
            description: "Level of avatar.",
            resolve: context => context.Source.Level);
        Field<NonNullGraphType<StringGraphType>>(
            nameof(ArenaParticipantStruct.NameWithHash),
            description: "Name of avatar.",
            resolve: context => context.Source.NameWithHash);
    }
}
