namespace Unity.Netcode.EditorTests
{
    internal class NopMessageSender : IMessageSender
    {
        public void Send(ulong clientId, NetworkDelivery delivery, ref FastBufferWriter batchData)
        {
        }
    }
}
