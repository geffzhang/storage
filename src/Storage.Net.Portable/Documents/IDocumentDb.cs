namespace Storage.Net.Documents
{
   /// <summary>
   /// Interface for a simplest possible document DB
   /// </summary>
   public interface IDocumentDb
   {
      /// <summary>
      /// Saves the document with specified ID
      /// </summary>
      void Save<T>(string id, T documentInstance);

      /// <summary>
      /// Loads a document by ID
      /// </summary>
      T Load<T>(string id);

      /// <summary>
      /// Checks if a document exists
      /// </summary>
      bool Exists<T>(string id);

      /// <summary>
      /// Deletes the document
      /// </summary>
      /// <returns>True if document was deleted, otherwise false.</returns>
      bool Delete<T>(string id);
   }
}
