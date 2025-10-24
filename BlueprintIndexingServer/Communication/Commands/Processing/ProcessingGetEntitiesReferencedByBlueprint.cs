using System.Linq;
using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingGetEntitiesReferencedByBlueprint
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        var payloadData = PayloadIdData.FromJson(command.Payload);
        var ids = payloadData?.Id != null ? database.GetEntitiesReferencedByBlueprint(payloadData.Id) : Enumerable.Empty<string>();
        var result = PayloadEntryListData.CreateEmpty();
        foreach (var id in ids)
        {
            result.Add(PayloadEntryData.Create(id, "", false, false));
        }
        return new Command { Type = CommandTypes.Result, Payload = result.ToJson() };
    }
}