using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TheoryCupids.Data;
using TheoryCupids.DTO;
using TheoryCupids.Helpers;
using TheoryCupids.Models;

namespace TheoryCupids.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Route("api/users/{userId}/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository _Repo;
        private readonly IMapper _Mapper;

        public MessagesController(IDatingRepository repo, IMapper mapper)
        {
            _Repo = repo;
            _Mapper = mapper;
        }

        [HttpGet("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var messageFromRepo = await _Repo.GetMessage(id);

            if(messageFromRepo == null) return NotFound();

            return Ok(messageFromRepo);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery] MessageParams messageParams)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            messageParams.UserId = userId;

            PagedList<Message> messagesFromRepo = await _Repo.GetMessagesForUser(messageParams);
            IEnumerable<MessageToReturnDTO> messages = _Mapper.Map<IEnumerable<MessageToReturnDTO>>(messagesFromRepo);

            Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize, messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

            return Ok(messages);
        }

        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            IEnumerable<Message> messageFromRepo = await _Repo.GetMessageThread(userId, recipientId);

            IEnumerable<MessageToReturnDTO> messageThread = _Mapper.Map<IEnumerable<MessageToReturnDTO>>(messageFromRepo);

            return Ok(messageThread);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDTO messageForCreationDTO)
        {
            // Check is user is making request from appropriate userId
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            messageForCreationDTO.SenderId = userId;

            // Check if recipient user exists
            User recipient = await _Repo.GetUser(messageForCreationDTO.RecipientId);
            if (recipient == null) return BadRequest("User not found");

            // Map the new message to the Message object and queue into entity
            Message message = _Mapper.Map<Message>(messageForCreationDTO);

            message.Recipient = recipient;
            message.Sender = await _Repo.GetUser(userId);

            _Repo.Add(message);

            // Execute save on the database and return a new route on the message
            if (await _Repo.SaveAll())
            {
                // Map the message back to the DTO
                MessageToReturnDTO messageToReturn = _Mapper.Map<MessageToReturnDTO>(message);
                return CreatedAtRoute("GetMessage", new { id = message.Id }, messageToReturn);
            }

            throw new System.Exception("Message failed to save");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            Message messageFromRepo = await _Repo.GetMessage(id);

            if (messageFromRepo.SenderId == userId)
                messageFromRepo.SenderDeleted = true;

            if (messageFromRepo.RecipientId == userId)
                messageFromRepo.RecipientDeleted = true;

            if (messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
                _Repo.Delete(messageFromRepo);

            if (await _Repo.SaveAll())
                return NoContent();

            throw new System.Exception("Error deleting message");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            Message message = await _Repo.GetMessage(id);

            if (message.RecipientId != userId)
                return Unauthorized();

            message.IsRead = true;
            message.DateRead = DateTime.Now;

            if (await _Repo.SaveAll())
                return NoContent();

            throw new Exception("Could not mark as read");
        }
    }
}
