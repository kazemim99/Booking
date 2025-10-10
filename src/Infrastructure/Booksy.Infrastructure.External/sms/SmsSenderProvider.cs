//using BookooEndUser.Service.UserService.Handlers.SMS;
//using BookooEndUser.Share.Result;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Bookoo.Layers.Infrastructure.External.sms
//{
//    public class SmsSenderProvider : ISmsSender
//    {
//        private readonly IList<string> _registeredSenders = new List<string>(0);

// public SmsSenderProvider() {
// _registeredSenders.Add(typeof(KavanegarSender).AssemblyQualifiedName); }

// ///
// <summary>
// /// create an instance of sender ///
// </summary>
// ///
// <param name="senderTypeName">Fully Qualified Sender type Name</param>
// ///
// <returns>ISmsSender</returns>
// private ISmsSender GetSender(string senderTypeName) { var type = Type.GetType(senderTypeName); if
// (type != null) return (ISmsSender)Activator.CreateInstance(type); foreach (var asm in
// AppDomain.CurrentDomain.GetAssemblies()) { type = asm.GetType(senderTypeName); if (type != null)
// return (ISmsSender)Activator.CreateInstance(type); } return null; }

//        public Task<IServiceCallResult> SendAsync(SMSRequest request)
//        {
//            throw new NotImplementedException();
//            //ISmsSender sende = this.GetInstance();
//        }
//    }
//}