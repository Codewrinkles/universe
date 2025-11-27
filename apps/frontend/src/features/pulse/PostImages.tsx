import type { PulseImage } from "../../types";

// Legacy type alias for compatibility
type PostImage = PulseImage;

interface PostImagesProps {
  images: PostImage[];
}

export function PostImages({ images }: PostImagesProps): JSX.Element {
  const count = images.length;

  if (count === 0) return <></>;

  // Single image - full width
  if (count === 1) {
    const image = images[0];
    if (!image) return <></>;
    return (
      <div className="mt-3 overflow-hidden rounded-2xl border border-border">
        <img
          src={image.url}
          alt={image.altText ?? "Post image"}
          className="w-full h-auto max-h-[512px] object-cover"
        />
      </div>
    );
  }

  // Two images - side by side
  if (count === 2) {
    return (
      <div className="mt-3 grid grid-cols-2 gap-0.5 overflow-hidden rounded-2xl border border-border">
        {images.map((image, index) => (
          <img
            key={index}
            src={image.url}
            alt={image.altText ?? `Post image ${index + 1}`}
            className="w-full h-64 object-cover"
          />
        ))}
      </div>
    );
  }

  // Three images - one large left, two stacked right
  if (count === 3) {
    const [first, second, third] = images;
    if (!first || !second || !third) return <></>;
    return (
      <div className="mt-3 grid grid-cols-2 gap-0.5 overflow-hidden rounded-2xl border border-border">
        <img
          src={first.url}
          alt={first.altText ?? "Post image 1"}
          className="w-full h-[272px] object-cover row-span-2"
        />
        <div className="flex flex-col gap-0.5">
          <img
            src={second.url}
            alt={second.altText ?? "Post image 2"}
            className="w-full h-[134px] object-cover"
          />
          <img
            src={third.url}
            alt={third.altText ?? "Post image 3"}
            className="w-full h-[134px] object-cover"
          />
        </div>
      </div>
    );
  }

  // Four+ images - 2x2 grid (only show first 4)
  return (
    <div className="mt-3 grid grid-cols-2 gap-0.5 overflow-hidden rounded-2xl border border-border">
      {images.slice(0, 4).map((image, index) => (
        <div key={index} className="relative">
          <img
            src={image.url}
            alt={image.altText ?? `Post image ${index + 1}`}
            className="w-full h-32 object-cover"
          />
          {index === 3 && count > 4 && (
            <div className="absolute inset-0 bg-black/60 flex items-center justify-center">
              <span className="text-white text-xl font-semibold">+{count - 4}</span>
            </div>
          )}
        </div>
      ))}
    </div>
  );
}
