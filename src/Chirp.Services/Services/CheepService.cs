using Chirp.Core.Classes;
using Chirp.Core.DTO;
using Chirp.Repositories.Interfaces;
using Chirp.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Chirp.Services
{
	public class CheepService : ICheepService
	{
		private readonly ICheepRepository _repository;
		private readonly ILogger<CheepService>? _logger;

		public CheepService(ICheepRepository repository, ILogger<CheepService>? logger = null)
		{
			_repository = repository;
			_logger = logger;
		}
		
		/// <summary>
		/// Used for getting all the cheeps from a specific page
		/// </summary>
		/// <param name="page"></param>
		/// <returns>A list of cheep DTO from the specific page</returns>
		public List<CheepDTO> GetCheeps(int page)
		{
			return _repository.GetCheeps(page);
		}
		
		/// <summary>
		/// Used for getting the all the cheeps from an author, by looking up their ID
		/// </summary>
		/// <param name="authorID"></param>
		/// <param name="page"></param>
		/// <returns>A list of cheeps from the specified user</returns>
		public List<CheepDTO> GetCheepsFromAuthorByID(string authorId, int page)
		{
			return _repository.GetCheepsFromAuthorByID(authorId, page);
		}
		
		/// <summary>
		/// Used for getting the cheeps by a specific author using the author's name for lookup
		/// </summary>
		/// <param name="authorName"></param>
		/// <param name="page"></param>
		/// <returns>A list of cheeps made by the specified author</returns>
		public List<CheepDTO> GetCheepsFromAuthorByName(string authorName, int page)
		{
			return _repository.GetCheepsFromAuthorByName(authorName, page);
		}

		/// <summary>
		/// Creates a new Cheep and associates it with an existing Author.
		/// </summary>
		/// <param name="newCheep">The CheepDTO object containing the details of the new Cheep to be created.</param>
		/// <returns>The ID of the newly created Cheep.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the Cheep text is longer than 160 characters or when the Author is not found.
		/// </exception>
		public int CreateCheep(CheepDTO newCheep)
		{
			var id = _repository.CreateCheep(newCheep);
			_logger?.LogInformation("Cheep {CheepId} created by author {AuthorId}", id, newCheep.Author.Id);
			return id;
		}
		
		/// <summary>
		/// Used for checking if an author is following another author
		/// </summary>
		/// <param name="followerAuthor"></param>
		/// <param name="followedAuthor"></param>
		/// <returns>True or false depending on if the author is following the other author</returns>
		public bool IsFollowing(AuthorDTO followerAuthor, AuthorDTO followedAuthor)
		{
			return _repository.IsFollowing(followerAuthor, followedAuthor);
		}
		
		/// <summary>
		/// Used for getting a list of the authors an author is following
		/// </summary>
		/// <param name="followerDTO"></param>
		/// <returns>a list of the authors an author is following</returns>
		public List<AuthorDTO> getFollowedInCheeps(AuthorDTO follower)
		{
			return _repository.getFollowedInCheeps(follower);
		}

		/// <summary>
		/// Used for getting the cheeps by a specific author using the author's ID for lookup
		/// </summary>
		/// <param name="followedAuthors"></param>
		/// <param name="currentUserID"></param>
		/// <param name="pageNumber"></param>
		/// <returns>A list of cheeps made by the specified author</returns>
		public List<CheepDTO> GetCheepsFromAuthors(List<AuthorDTO> followedAuthors, string currentUserID, int pageNumber)
		{
			return _repository.GetCheepsFromAuthors(followedAuthors, currentUserID, pageNumber);
		}
		
		/// <summary>
		/// Used for following another user
		/// </summary>
		/// <param name="followerAuthor"></param>
		/// <param name="followedAuthor"></param>
		public void Follow(AuthorDTO followerAuthor, AuthorDTO followedAuthor)
		{
			_repository.Follow(followerAuthor, followedAuthor);
			_logger?.LogInformation("Author {FollowerId} followed {FollowedId}", followerAuthor.Id, followedAuthor.Id);
		}

		/// <summary>
		/// Used for unfollowing another user
		/// </summary>
		/// <param name="followerAuthor"></param>
		/// <param name="followedAuthor"></param>
		/// <exception cref="ArgumentException"></exception>
		public void Unfollow(AuthorDTO followerAuthor, AuthorDTO followedAuthor)
		{
			_repository.Unfollow(followerAuthor, followedAuthor);
			_logger?.LogInformation("Author {FollowerId} unfollowed {FollowedId}", followerAuthor.Id, followedAuthor.Id);
		}

		/// <summary>
		/// Used for getting an author object by its ID
		/// </summary>
		/// <param name="authorId"></param>
		/// <returns>The author with the specified ID</returns>
		public AuthorDTO? GetAuthorByID(string authorId)
		{
			return _repository.GetAuthorByID(authorId);
		}

		/// <summary>
		/// Used for getting an author object by its name
		/// </summary>
		/// <param name="authorName"></param>
		/// <returns>The author with the specified name</returns>
		public AuthorDTO? GetAuthorByName(string authorName)
		{
			return _repository.GetAuthorByName(authorName);
		}

		/// <summary>
		/// Used for getting an author object by its email
		/// </summary>
		/// <param name="email"></param>
		/// <returns>The author with the specified name</returns>
		/// <exception cref="ArgumentException"></exception>
		public AuthorDTO? GetAuthorByEmail(string email)
		{
			return _repository.GetAuthorByEmail(email);
		}
		
		/// <summary>
		/// Used for getting the amount of cheeps in the database
		/// </summary>
		/// <returns>Amount of cheeps as an int</returns>
		public int GetCheepCount()
		{
			return _repository.GetCheepCount();
		}
		
		/// <summary>
		/// Used for getting the amount of cheeps by an author when you know the authorID
		/// </summary>
		/// <param name="authorId"></param>
		/// <returns>Amount of cheeps as an int</returns>
		public int GetCheepCountByID(string authorId)
		{
			return _repository.GetCheepCountByID(authorId);
		}
		
		/// <summary>
		/// Used for getting the amount of cheeps by an author when you have the name of the author
		/// </summary>
		/// <param name="authorName"></param>
		/// <returns>Amount of cheeps as an int</returns>
		public int GetCheepByName(string authorName)
		{
			return _repository.GetCheepCountByName(authorName);
		}

		/// <summary>
		/// Converts a DTO author to the domain object
		/// </summary>
		/// <param name="author"></param>
		/// <returns>The domain object</returns>
		public Author ToDomain(AuthorDTO author)
		{
			return _repository.ToDomain(author);
		}
		
		/// <summary>
		/// Used for getting the amount of cheeps by a list of authors when you have the author ID of the current user
		/// </summary>
		/// <param name="followedAuthors"></param>
		/// <param name="currentUserId"></param>
		/// <returns>Amount of cheeps combined for all the authors in the list + the current users amount</returns>
		public int GetCheepCountByAuthors(List<AuthorDTO> followedAuthors, string currentUserId)
		{
			return _repository.GetCheepCountByAuthors(followedAuthors, currentUserId);
		}
		
		/// <summary>
		/// Used for updating a cheep
		/// </summary>
		/// <param name="newCheep"></param>
		/// <param name="cheepID"></param>
		public void UpdateCheep(CheepDTO newCheep, int cheepID)
		{
			_repository.UpdateCheep(newCheep, cheepID);
			_logger?.LogInformation("Cheep {CheepId} updated", cheepID);
		}

		/// <summary>
		/// Used for deleting a cheep by its ID
		/// </summary>
		/// <param name="cheepID"></param>
		public void DeleteCheep(int cheepId)
		{
			_repository.DeleteCheep(cheepId);
			_logger?.LogInformation("Cheep {CheepId} deleted", cheepId);
		}
		
		/// <summary>
		/// Used for getting a cheep by its ID
		/// </summary>
		/// <param name="cheepID"></param>
		/// <returns>The cheep with the ID</returns>
		/// <exception cref="ArgumentException"></exception>
		public CheepDTO GetCheepByID(int cheepId)
		{
			return _repository.GetCheepByID(cheepId);
		}

		/// <summary>
		/// Used for getting all the comments from a cheep
		/// </summary>
		/// <param name="cheepId"></param>
		/// <returns>A list of comment DTO from the cheep</returns>
		public List<CommentDTO> GetCommentsForCheep(int cheepId)
		{
			return _repository.GetCommentsForCheep(cheepId);
		}

		/// <summary>
		/// Used for getting the amount of comments for a single cheep
		/// </summary>
		/// <param name="cheepId"></param>
		/// <returns>The amount of comments on the specified cheep</returns>
		public int GetCommentCountForCheep(int cheepId)
		{
			return _repository.GetCommentsForCheep(cheepId).Count;
		}

		/// <summary>
		/// Used for adding a comment to a cheep
		/// </summary>
		/// <param name="comment"></param>
		/// <exception cref="ArgumentException"></exception>
		public void AddComment(CommentDTO comment)
		{
			_repository.AddComment(comment);
			_logger?.LogInformation("Comment added by author {AuthorId} on cheep {CheepId}", comment.Author.Id, comment.CheepId);
		}
		
		/// <summary>
		/// Deletes a specific comment using its ID
		/// </summary>
		/// <param name="commentId"></param>
		public void DeleteComment(int commentId)
		{
			_repository.DeleteComment(commentId);
			_logger?.LogInformation("Comment {CommentId} deleted", commentId);
		}
	}
}