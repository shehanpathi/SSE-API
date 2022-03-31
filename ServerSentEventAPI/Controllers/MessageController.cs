using core.notification.Extentions;
using core.notification.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServerSentEventAPI.Message;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServerSentEventAPI.Controllers
{
    [ApiController]
    public class MessageController : ControllerBase
    {
		private readonly IMessageQueue MessageQueue;

        public MessageController(IMessageQueue messageQueue)
        {
			MessageQueue = messageQueue;
		}

		[HttpGet("/sse")]
		public async Task Subscribe(string cif)
		{
			Response.ContentType = "text/event-stream";
			Response.StatusCode = 200;

			StreamWriter streamWriter = new StreamWriter(Response.Body);

			MessageQueue.Register(cif);

			try
			{
				//await MessageQueue.EnqueueAsync(new SSEData { CIF = "000", Body = null, Title = null }, HttpContext.RequestAborted);

				await foreach (var message in MessageQueue.DequeueAsync(cif, HttpContext.RequestAborted))
				{
					await streamWriter.WriteLineAsync($"data: {DateTime.Now} {message}\n");
					await streamWriter.FlushAsync();
				}
			}
			catch (OperationCanceledException)
			{
				//this is expected when the client disconnects the connection
			}
			catch (Exception)
			{
				Response.StatusCode = 400;
			}
			finally
			{
				MessageQueue.Unregister(cif);
			}
		}



		[HttpGet("/messages/sse")]
		public async Task SimpleSSE(string cif)
		{
            //1. Set content type
            Response.ContentType = "text/event-stream";
            Response.StatusCode = 200;

            StreamWriter streamWriter = new StreamWriter(Response.Body);

            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                //2. Await something that generates messages
                await Task.Delay(5000, HttpContext.RequestAborted);

                //3. Write to the Response.Body stream
                await streamWriter.WriteLineAsync($"data: {DateTime.Now} Looping \n");
                await streamWriter.FlushAsync();

            }
        }

		[HttpGet("/messages/http/sse")]
		public async Task SimpleHttpSSE(string cif)
		{
			////1. Set content type
			////HttpContext.Response.StatusCode = 200;
			////HttpContext.Response.Headers.Add("Cache-Control", "no-cache");
			////HttpContext.Response.Headers.Add("Connection", "keep-alive");
			//HttpContext.Response.Headers.Add("Content-Type", "text/event-stream");

			//while (!HttpContext.RequestAborted.IsCancellationRequested)
			//{
			//	//2. Await something that generates messages
			//	await Task.Delay(5000, HttpContext.RequestAborted);

			//	//3. Write to the Response.Body stream
			//	await HttpContext.Response.WriteAsync($"data: {DateTime.Now} Looping \n");
			//	await HttpContext.Response.Body.FlushAsync();

			//}

		}

		[HttpPost]
		[Route("messages")]
		public async Task<IActionResult> PostMessage([FromBody] SSEData messageRequest)
		{
			try
			{
				await MessageQueue.EnqueueAsync(messageRequest, HttpContext.RequestAborted);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}



	}
}
