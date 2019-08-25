using System.Collections.Generic;

namespace Collections.Unsafe {
  public interface IUnsafeIterator<T> : IEnumerator<T>, IEnumerable<T> where T : struct {

  }
}