import { Skeleton } from '@/shared/components'
import { tableStyles } from './order-table-styles'

interface OrderTableSkeletonProps {
  rows?: number
}

export function OrderTableSkeleton({ rows = 5 }: OrderTableSkeletonProps) {
  return (
    <>
      {Array.from({ length: rows }).map((_, i) => (
        <tr key={i} className="h-16 border-b border-slate-200">
          <td className={tableStyles.cell}>
            <div className={tableStyles.cellFlex}>
              <Skeleton className="h-4 w-16" />
            </div>
          </td>
          <td className={tableStyles.cell}>
            <div className={tableStyles.cellClient}>
              <Skeleton className="h-4 w-full max-w-[12rem]" />
            </div>
          </td>
          <td className={tableStyles.cellRight}>
            <div className={tableStyles.cellFlexEnd}>
              <Skeleton className="h-4 w-20" />
            </div>
          </td>
          <td className={tableStyles.cell}>
            <div className={tableStyles.cellFlex}>
              <Skeleton className="h-4 w-24" />
            </div>
          </td>
          <td className={tableStyles.cell}>
            <div className={tableStyles.cellFlexCenter}>
              <Skeleton className="h-5 w-20 rounded-full" />
            </div>
          </td>
          <td className={tableStyles.cell}>
            <div className={tableStyles.cellFlexCenter}>
              <Skeleton className="h-7 w-24 rounded-md" />
            </div>
          </td>
        </tr>
      ))}
    </>
  )
}
