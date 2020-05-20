namespace GZipTest
{
	/// <summary>
	/// Processing module interface
	/// </summary>
	interface IProcessingModule
	{
		/// <summary>
		/// Indicates wether the module is processing a file
		/// </summary>
		bool IsWorking { get; }

		/// <summary>
		/// Starts the job
		/// </summary>
		/// <param name="input">Source file path (relative or absolute)</param>
		/// <param name="output">Destination file path (relative or absolute)</param>
		void Run(string input, string output);
	}
}