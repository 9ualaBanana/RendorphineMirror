import { useState, useEffect } from 'react';

const App = () => {
  const [imageIds, setImageIds] = useState([]);
  const [images, setImages] = useState({});

  useEffect(() => {
    // Fetch the image IDs from the /ids endpoint
    const fetchImageIds = async () => {
      const response = await fetch('http://localhost:5336/ids');
      const ids = (await response.json()).ids;
      setImageIds(ids);
    };
    fetchImageIds();
  }, []);

  useEffect(() => {
    // Fetch the images from the /rfpreview/{id} endpoint
    const fetchImage = async (id: string) => {
      const response = await fetch(`http://localhost:5336/rfpreview/${id}`);
      const imageData = await response.blob();
      setImages((prevImages) => ({ ...prevImages, [id]: imageData }));
    };

    imageIds.forEach((id) => {
      if (!images[id]) {
        fetchImage(id);
      }
    });
  }, [imageIds, images]);

  return (
    <div className="grid-container" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(500px, 1fr))', gridAutoRows: 'auto', width: '100vw' }}>
      {imageIds.map((id) => (
        <div key={id} className="grid-item">
          {images[id] && <img src={URL.createObjectURL(images[id])} alt={`Image ${id}`} />}
        </div>
      ))}
    </div>
  );
};

export default App;
